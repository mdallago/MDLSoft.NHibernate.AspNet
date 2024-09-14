using System.Web;
//using System.Web.Mvc;
using log4net;
using MDLSoft.NHibernate.MultiSessionFactory;
using NHibernate;

namespace MDLSoft.NHibernate.AspNet
{
    public class NHSessionWebModule : IHttpModule
    {
        public const string SESSION_FACTORY_PROVIDER_KEY = "NHSession.SessionFactoryProvider";
        private static readonly ILog log = LogManager.GetLogger(typeof(NHSessionWebModule));
        private ISessionFactoryProvider sfp;

        public static void Setup(ISessionFactoryProvider sessionFactoryProvider)
        {
            HttpContext.Current.Application[SESSION_FACTORY_PROVIDER_KEY] = sessionFactoryProvider;
        }

        public void Init(HttpApplication context)
        {
            log.Debug("Obtaining SessionFactoryProvider from Application context.");

            sfp = context.Application[SESSION_FACTORY_PROVIDER_KEY] as ISessionFactoryProvider;


            if (sfp == null)
            {
                throw new HibernateException("Couldn't obtain SessionFactoryProvider from WebApplicationContext.");
            }

            context.PreRequestHandlerExecute += (o, e) =>
            {
                foreach (ISessionFactory factory in sfp)
                {
                    factory.GetCurrentSession().BeginTransaction();
                }
            };

            context.EndRequest += (o, e) =>
                                      {
                                          foreach (ISessionFactory factory in sfp)
                                          {
                                              ISession session = factory.GetCurrentSession();

                                              if (session != null)
                                              {
                                                  var transaction = session.GetCurrentTransaction();

                                                  try
                                                  {
                                                      if (transaction != null)
                                                      {
                                                          if (session.IsOpen && transaction.IsActive)
                                                          {
                                                              transaction.Commit();
                                                          }
                                                      }
                                                  }
                                                  catch
                                                  {
                                                      if (transaction != null)
                                                      {
                                                          transaction.Rollback();
                                                      }
                                                      throw;
                                                  }
                                                  finally
                                                  {
                                                      session.Dispose();
                                                  }
                                              }
                                          }
                                      };
        }

        public void Dispose()
        {

        }
    }
}

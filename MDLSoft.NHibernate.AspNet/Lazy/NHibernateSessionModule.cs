using System;
using System.Net;
using System.Web;
using MDLSoft.NHibernate.MultiSessionFactory;
//using log4net;
using NHibernate;

namespace MDLSoft.NHibernate.AspNet.Lazy
{
    public class NHibernateSessionModule : IHttpModule
    {
        //private static readonly ILog Log = LogManager.GetLogger(typeof(NHibernateSessionModule));
        private ISessionFactoryProvider sfp;

        public void Init(HttpApplication context)
        {
            context.BeginRequest += ContextBeginRequest;
            context.EndRequest += ContextEndRequest;

            sfp = (ISessionFactoryProvider)context.Application[SessionFactoryProviderKeys.KEY];
        }

        public void Dispose()
        {

        }

        private void ContextBeginRequest(object sender, EventArgs e)
        {
            foreach (var sf in sfp)
            {
                var localFactory = sf;
                LazySessionContext.Bind(
                    new Lazy<ISession>(() => BeginSession(localFactory)),
                    sf);
            }
        }

        private static ISession BeginSession(ISessionFactory sf)
        {
            var session = sf.OpenSession();
            session.BeginTransaction();
            return session;
        }

        private void ContextEndRequest(object sender, EventArgs e)
        {
            HttpContext context = ((HttpApplication)sender).Context;

            if (context != null && context.Items.Contains(SessionFactoryProviderKeys.KEY_EXCEPTION))
            {
                Rollback();
                return;
            }

            foreach (var sf in sfp)
            {
                var session = LazySessionContext.UnBind(sf);
                if (session == null)
                    continue;
                EndSession(session);
            }
        }

        private void Rollback()
        {
            foreach (var sf in sfp)
            {
                var s = sf.GetCurrentSession();

                var transction = s.GetCurrentTransaction();

                if (transction != null && transction.IsActive)
                {
                    transction.Rollback();
                }
            }
        }

        private void EndSession(ISession session)
        {
            try
            {
                var transaction = session.GetCurrentTransaction();

                if (transaction != null && transaction.IsActive)
                {
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                //Log.Error("Error guadardando datos en la DB ", ex);
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                HttpContext.Current.Response.Write("Error al procesar la solicitud");
            }
            finally
            {
                session.Dispose();
            }
        }
    }
}

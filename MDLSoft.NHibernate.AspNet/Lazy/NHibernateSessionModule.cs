using System;
using System.Net;
using System.Web;
using MDLSoft.NHibernate.MultiSessionFactory;
using log4net;
using NHibernate;

namespace MDLSoft.NHibernate.AspNet.Lazy
{
    public class NHibernateSessionModule : IHttpModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NHibernateSessionModule));
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
            if (sfp == null)
            {
                Log.Warn("Session factory provider not configured");
                return;
            }

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
            if (sfp == null)
            {
                Log.Warn("Session factory provider not configured");
                return;
            }

            HttpContext context = ((HttpApplication)sender).Context;
            bool rollback = context != null && context.Items.Contains(SessionFactoryProviderKeys.KEY_EXCEPTION);

            foreach (var sf in sfp)
            {
                var session = LazySessionContext.UnBind(sf);
                if (session == null)
                    continue;
                EndSession(session, rollback);
            }
        }

        private static void EndSession(ISession session, bool rollback)
        {
            try
            {
                var transaction = session.GetCurrentTransaction();

                if (transaction != null && transaction.IsActive)
                {
                    if (rollback)
                    {
                        transaction.Rollback();
                    }
                    else
                    {
                        transaction.Commit();
                    }
                }
            }
            /*catch (Exception ex)
            {
                Log.Error("Error commiting transaction", ex);
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                HttpContext.Current.Response.Write("Error processing request");
            }*/
            finally
            {
                session.Dispose();
            }
        }
    }
}

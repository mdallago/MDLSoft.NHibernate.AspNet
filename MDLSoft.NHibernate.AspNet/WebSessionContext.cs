using System;
using System.Collections.Generic;
using System.Web;
using MDLSoft.NHibernate.MultiSessionFactory;
using NHibernate;
using NHibernate.Engine;

namespace MDLSoft.NHibernate.AspNet
{
    [Serializable]
    public class WebSessionContext : CurrentSessionContext
    {
        public const string SESSION_FACTORY_KEY = "NHSession.Context.TransactedSessionContext";

        public WebSessionContext(ISessionFactoryImplementor factory) : base(factory) { }

        #region Overrides of AbstractCurrentSessionContext

        protected override IDictionary<ISessionFactory, ISession> GetContextDictionary()
        {
            return HttpContext.Current.Items[SESSION_FACTORY_KEY] as IDictionary<ISessionFactory, ISession>;
        }

        protected override void SetContextDictionary(IDictionary<ISessionFactory, ISession> value)
        {
            HttpContext.Current.Items[SESSION_FACTORY_KEY] = value;
        }

        #endregion
    }
}
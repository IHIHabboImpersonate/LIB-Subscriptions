using IHI.Database;
using IHI.Server.Extras;
using IHI.Server.Habbos;
using NHibernate.Criterion;

namespace IHI.Server.Libraries.Cecer1.Subscriptions
{
    public class SubscriptionData
    {
        private readonly Habbo _habbo;
        private readonly Subscription _subscriptionDatabase;

        public SubscriptionData(Habbo habbo, string type)
        {
            _habbo = habbo;

            using (var db = CoreManager.GetServerCore().GetDatabaseSession())
            {
                _subscriptionDatabase = db.CreateCriteria<Subscription>().
                    Add(Restrictions.Eq("habbo_id", _habbo.GetID())).
                    Add(Restrictions.Eq("subscription_type", type)).
                    UniqueResult<Subscription>();
            }
            if (_subscriptionDatabase != null) return;
            _subscriptionDatabase = new Subscription {habbo_id = habbo.GetID()};
        }

        public int GetRemainingSeconds()
        {
            if (_subscriptionDatabase.skipped_length == 0)
                return 0; // Not started yet.
            return UsefulStuff.GetUnixTimpstamp() - _subscriptionDatabase.total_bought +
                   _subscriptionDatabase.skipped_length;
        }

        public int GetExpiredSeconds()
        {
            if (_subscriptionDatabase.skipped_length == 0)
                return 0; // Not started yet.
            return UsefulStuff.GetUnixTimpstamp() - _subscriptionDatabase.skipped_length;
        }

        public SubscriptionData SetRemainingSeconds(int seconds)
        {
            _subscriptionDatabase.total_bought = UsefulStuff.GetUnixTimpstamp() - _subscriptionDatabase.skipped_length +
                                                 seconds;
            return this;
        }

        public SubscriptionData SetExpiredSeconds(int seconds)
        {
            _subscriptionDatabase.skipped_length = UsefulStuff.GetUnixTimpstamp() - seconds;
            return this;
        }

        public SubscriptionData AddSubscriptionSeconds(int seconds)
        {
            _subscriptionDatabase.total_bought += seconds;
            return this;
        }

        public bool IsActive()
        {
            if (_subscriptionDatabase.paused_start == 0)
                return true;
            return false;
        }

        public SubscriptionData SetActive(bool active)
        {
            if (active == IsActive())
                return this; // Nothing to do

            if (!active)
            {
                _subscriptionDatabase.paused_start = UsefulStuff.GetUnixTimpstamp();
                return this;
            }
            _subscriptionDatabase.skipped_length += UsefulStuff.GetUnixTimpstamp() - _subscriptionDatabase.paused_start;
            _subscriptionDatabase.paused_start = 0;
            return this;
        }

        public SubscriptionData SaveChanges()
        {
            using (var db = CoreManager.GetServerCore().GetDatabaseSession())
            {
                var s = db.Get<Subscription>(_habbo.GetID());
                s.paused_start = _subscriptionDatabase.paused_start;
                s.skipped_length = _subscriptionDatabase.skipped_length;
                s.total_bought = _subscriptionDatabase.total_bought;

                db.SaveOrUpdate(s);
            }
            return this;
        }
    }
}
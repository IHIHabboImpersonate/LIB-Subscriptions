using System;
using System.Linq;
using IHI.Server.Habbos;
using IHI.Server.Extras;
using IHI.Database;

using NHibernate;

namespace IHI.Server.Plugin.Cecer1.Subscriptions
{
    public class SubscriptionData
    {
        private Habbos.Habbo fHabbo;
        private Subscription fSubscriptionDatabase;

        public SubscriptionData(Habbos.Habbo Habbo, string Type)
        {
            this.fHabbo = Habbo;

            try
            {
                using (ISession DB = CoreManager.GetCore().GetDatabaseSession())
                {
                    this.fSubscriptionDatabase = DB.CreateCriteria<Database.Subscription>().
                        Add(new NHibernate.Criterion.EqPropertyExpression("habbo_id", this.fHabbo.GetID().ToString())).
                        Add(new NHibernate.Criterion.EqPropertyExpression("subscription_type", Type)).
                        List<Database.Subscription>().First();
                }
            }
            catch (ArgumentNullException)
            {
                this.fSubscriptionDatabase = new Database.Subscription();
                this.fSubscriptionDatabase.habbo_id = Habbo.GetID();
            }
        }

        public int GetRemainingSeconds()
        {
            return UsefulStuff.GetUnixTimpstamp() - this.fSubscriptionDatabase.total_bought + this.fSubscriptionDatabase.skipped_length;
        }
        public int GetExpiredSeconds()
        {
            return UsefulStuff.GetUnixTimpstamp() - this.fSubscriptionDatabase.skipped_length;
        }
        public SubscriptionData SetRemainingSeconds(int Seconds)
        {
            this.fSubscriptionDatabase.total_bought = UsefulStuff.GetUnixTimpstamp() - this.fSubscriptionDatabase.skipped_length + Seconds;
            return this;
        }
        public SubscriptionData SetExpiredSeconds(int Seconds)
        {
            this.fSubscriptionDatabase.skipped_length = UsefulStuff.GetUnixTimpstamp() - Seconds;
            return this;
        }
        public SubscriptionData AddSubscriptionSeconds(int Seconds)
        {
            this.fSubscriptionDatabase.total_bought += Seconds;
            return this;
        }

        public bool IsActive()
        {
            if (this.fSubscriptionDatabase.paused_start == 0)
                return true;
            return false;
        }

        public SubscriptionData SetActive(bool Active)
        {
            if (Active == IsActive())
                return this; // Nothing to do

            if (!Active)
            {
                this.fSubscriptionDatabase.paused_start = UsefulStuff.GetUnixTimpstamp();
                return this;
            }
            this.fSubscriptionDatabase.skipped_length += UsefulStuff.GetUnixTimpstamp() - this.fSubscriptionDatabase.paused_start;
            this.fSubscriptionDatabase.paused_start = 0;
            return this;
        }

        public SubscriptionData SaveChanges()
        {
            using (ISession DB = CoreManager.GetCore().GetDatabaseSession())
            {
                Database.Subscription S = DB.Get<Database.Subscription>(this.fHabbo.GetID());
                S.paused_start = this.fSubscriptionDatabase.paused_start;
                S.skipped_length = this.fSubscriptionDatabase.skipped_length;
                S.total_bought = this.fSubscriptionDatabase.total_bought;

                DB.SaveOrUpdate(S);
            }
            return this;
        }
    }
}
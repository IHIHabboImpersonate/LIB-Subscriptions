#region GPLv3

// 
// Copyright (C) 2012  Chris Chenery
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

#endregion

#region Usings

using IHI.Database;
using IHI.Server.Extras;
using IHI.Server.Habbos;
using NHibernate;
using NHibernate.Criterion;

#endregion

namespace IHI.Server.Libraries.Cecer1.Subscriptions
{
    public class SubscriptionData
    {
        private readonly Habbo _habbo;
        private readonly Subscription _subscriptionDatabase;

        public SubscriptionData(Habbo habbo, string type)
        {
            _habbo = habbo;

            using (ISession db = CoreManager.ServerCore.GetDatabaseSession())
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
            using (ISession db = CoreManager.ServerCore.GetDatabaseSession())
            {
                Subscription s = db.Get<Subscription>(_habbo.GetID());
                s.paused_start = _subscriptionDatabase.paused_start;
                s.skipped_length = _subscriptionDatabase.skipped_length;
                s.total_bought = _subscriptionDatabase.total_bought;

                db.SaveOrUpdate(s);
            }
            return this;
        }
    }
}
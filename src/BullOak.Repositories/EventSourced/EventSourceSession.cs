﻿namespace BullOak.Repositories.EventSourced
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class EventSourceSession<TState, TConcurrency> : BaseSession<TState>, IManageEventSourceSession<TState>
        where TState : new()
    {
        private IHoldEventWithMetadata[] events;
        private int initialized = 0;
        private TConcurrency concurrencyId;

        public IEnumerable<IHoldEventWithMetadata> EventStream { get; private set; }

        protected EventSourceSession(ICreateEventAppliers eventAppliersFactory)
            :base(eventAppliersFactory)
        { }

        public void Initialize(IHoldEventWithMetadata[] events, TConcurrency concurrencyId)
        {
            if (Interlocked.Increment(ref initialized) == 1)
            {
                this.events = events;
                this.concurrencyId = concurrencyId;
                this.EventStream = events.ToList().AsReadOnly();
            }
            else
            {
                throw new Exception("Already initialized.");
            }
        }

        public sealed override Task SaveChanges()
            => SaveEvents(base.eventsToStore, concurrencyId);

        protected abstract Task SaveEvents(List<IHoldEventWithMetadata> newEvents, TConcurrency concurrency);

        protected sealed override TState GetCurrent()
        {
            if (events == null) throw new ArgumentNullException("Not initialized. Please make sure that existing events are set first");

            var state = new TState();
            int i;
            for (i = 0; i < events.Length; i++)
            {
                state = ApplyEvent(state, events[i]);
            }
            for (i = 0; i < eventsToStore.Count; i++)
            {
                state = ApplyEvent(state, (dynamic)eventsToStore[i]);
            }
            return state;
        }

    }
}
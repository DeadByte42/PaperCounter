using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace PaperCounter.Utility
{
	public enum EventMode
	{
		Raise,
		Ignore,
		Suspend
	}

	public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        private bool wasUpdated = false;
        private EventMode eventMode = EventMode.Raise;
        public EventMode EventMode
        {
            get
            {
                return eventMode;
            }
            set
            {
                eventMode = value;
                if (wasUpdated)
                {
                    if (eventMode == EventMode.Raise)
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    if (eventMode != EventMode.Suspend)
                        wasUpdated = false;
                }
            }
        }


        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged newItem in e.NewItems)
                {
                    newItem.PropertyChanged += ItemsPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged oldItem in e.OldItems)
                {
                    oldItem.PropertyChanged -= ItemsPropertyChanged;
                }
            }

            if (EventMode == EventMode.Ignore)
                return;
            if (EventMode == EventMode.Suspend)
            {
                wasUpdated = true;
                return;
            }
            base.OnCollectionChanged(e);
        }


        public event PropertyChangedEventHandler ItemsPropertyChanged;
    }
}

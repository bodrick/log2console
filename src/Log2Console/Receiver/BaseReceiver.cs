using System;
using System.ComponentModel;
using Log2Console.Log;
using Newtonsoft.Json;

namespace Log2Console.Receiver
{
    public abstract class BaseReceiver : MarshalByRefObject, IReceiver
    {
        protected ILogMessageNotifiable Notifiable;

        #region IReceiver Members

        [JsonIgnore]
        public abstract string SampleClientConfig { get; }

        [Browsable(false)]
        public string DisplayName { get; protected set; }

        public abstract void Initialize();
        public abstract void Terminate();

        public virtual void Attach(ILogMessageNotifiable notifiable) => Notifiable = notifiable;

        public virtual void Detach() => Notifiable = null;

        #endregion IReceiver Members
    }
}
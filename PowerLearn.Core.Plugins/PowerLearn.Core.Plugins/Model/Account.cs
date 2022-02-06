namespace PowerLearn.Core.Plugins.Model
{
    using System.Runtime.Serialization;

    [DataContract]
    public class Account
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Website { get; set; }
    }
}

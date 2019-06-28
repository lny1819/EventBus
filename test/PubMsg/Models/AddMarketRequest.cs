

namespace PubMessage.Models
{
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name = @"AddMarketRequest")]
    public partial class AddMarketRequest : global::ProtoBuf.IExtensible
    {
        public AddMarketRequest() { }

        private string _UserID = "";
        [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name = @"UserID", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string UserID
        {
            get { return _UserID; }
            set { _UserID = value; }
        }
        private string _ContractID = "";
        [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name = @"ContractID", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string ContractID
        {
            get { return _ContractID; }
            set { _ContractID = value; }
        }
        private int _RequestID = default(int);
        [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name = @"RequestID", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int RequestID
        {
            get { return _RequestID; }
            set { _RequestID = value; }
        }
        private string _Exchange = "";
        [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name = @"Exchange", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string Exchange
        {
            get { return _Exchange; }
            set { _Exchange = value; }
        }
        private string _Commidity = "";
        [global::ProtoBuf.ProtoMember(5, IsRequired = false, Name = @"Commidity", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string Commidity
        {
            get { return _Commidity; }
            set { _Commidity = value; }
        }
        private global::ProtoBuf.IExtension extensionObject;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
}

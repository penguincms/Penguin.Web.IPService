namespace Penguin.Web.IPServices.Arin
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class NetBlock
    {
        public int CidrLenth { get; set; }
        public string EndAddress { get; set; }
        public string Type { get; set; }
        public string StartAddress { get; set; }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
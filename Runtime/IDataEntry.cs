using Fsi.Localization;

namespace Fsi.DataSystem
{
    public interface IDataEntry<out T>
    {
        public T ID { get; }
        
        public LocEntry LocName { get; }
        public string Name => LocName.GetLocalizedString("no_loc_name");
        
        public LocEntry LocDesc { get; }
        public string Desc => LocDesc.GetLocalizedString("no_loc_desc");
    }
}
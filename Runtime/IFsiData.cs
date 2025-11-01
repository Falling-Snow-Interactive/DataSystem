using Fsi.Localization;

namespace Fsi.DataSystem
{
    public interface IFsiData<out T>
    {
        public T Id { get; }
        
        public LocEntry LocName { get; }
        public string Name => LocName.GetLocalizedString("no_loc_name");
        
        public LocEntry LocDesc { get; }
        public string Desc => LocDesc.GetLocalizedString("no_loc_desc");
    }
}
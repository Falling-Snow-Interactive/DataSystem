namespace Fsi.DataSystem.Libraries
{
	public interface ILibraryData<out T>
	{
		public T ID { get; }
	}
}
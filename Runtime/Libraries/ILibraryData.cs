namespace Fsi.DataSystem.Libraries
{
	public interface ILibraryData<out T>
	{
		public T ID { get; }

		#if UNITY_EDITOR

		public void Validate();

		#endif
	}
}
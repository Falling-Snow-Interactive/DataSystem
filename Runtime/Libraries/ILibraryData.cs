namespace Fsi.DataSystem.Libraries
{
	public interface ILibraryData<out T>
	{
		public T ID { get; }

		#if UNITY_EDITOR

		// TODO - Make it part of the validator - Kira
		// public void Validate();

		#endif
	}
}
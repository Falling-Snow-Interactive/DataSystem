namespace Fsi.DataSystem.Libraries
{
	/// <summary>
	/// Defines a library data entry with a stable identifier.
	/// </summary>
	/// <typeparam name="T">The identifier type.</typeparam>
	public interface IData<out T>
	{
		/// <summary>
		/// Gets the identifier for this entry.
		/// </summary>
		public T ID { get; }

		public string ToString();
	}
}

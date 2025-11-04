namespace Fsi.DataSystem.Selectors
{
	public interface ISelectorData<out T>
	{
		public T ID { get; }
	}
}
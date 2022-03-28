using ExceptionSupplement;


ThrowHelper.ThrowArgumentNull();

Console.WriteLine();


[ThrowException(typeof(ArgumentException))]
[ThrowException(typeof(ArgumentNullException))]
static partial class ThrowHelper
{

}

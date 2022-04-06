using ExceptionSupplement;


ThrowHelper.ThrowArgumentNull();

Console.WriteLine();


[ThrowException(typeof(ArgumentException))]
[ThrowException(typeof(ArgumentNullException))]
static partial class ThrowHelper
{

}

[ThrowException(typeof(ArgumentException), false)]
[ThrowException(typeof(ArgumentNullException), false)]
static partial class Throw
{

}

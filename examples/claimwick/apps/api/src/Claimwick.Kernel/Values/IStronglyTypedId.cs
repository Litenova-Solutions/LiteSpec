namespace Claimwick.Kernel.Values;

/// <summary>One strongly typed Guid identity every module reads the same way.</summary>
public interface IStronglyTypedId
{
    /// <summary>The Guid value.</summary>
    Guid Value { get; }
}

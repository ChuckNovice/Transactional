namespace Transactional.Abstractions;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Helper class for invoking transaction callbacks.
/// </summary>
internal static class TransactionCallbackInvoker
{
    /// <summary>
    /// Invokes all callbacks in order, collecting any exceptions and throwing them as an AggregateException.
    /// </summary>
    /// <param name="callbacks">The callbacks to invoke.</param>
    /// <param name="cancellationToken">A token to pass to each callback.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal static async Task InvokeAsync(
        List<Func<CancellationToken, Task>> callbacks,
        CancellationToken cancellationToken)
    {
        if (callbacks.Count == 0)
        {
            return;
        }

        List<Exception>? exceptions = null;

        foreach (var callback in callbacks)
        {
            try
            {
                await callback(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }
        }

        if (exceptions != null)
        {
            throw new AggregateException(
                "One or more transaction callbacks threw an exception.",
                exceptions);
        }
    }
}

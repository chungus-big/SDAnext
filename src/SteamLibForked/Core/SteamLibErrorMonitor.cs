using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using SteamLib.Exceptions.General;

namespace SteamLib.Core;

/// <summary>
///     Provides unified error handling and mapping for responses from Steam.
/// </summary>
public static class SteamLibErrorMonitor
{
    [DoesNotReturn]
    internal static void MapAndThrowException(string response, Exception ex)
    {
        var e = new UnsupportedResponseException(response, ex);
        ExceptionDispatchInfo.SetCurrentStackTrace(e);
        throw e;
    }

    [DoesNotReturn]
    internal static T MapAndThrowException<T>(string response, Exception ex)
    {
        var e = new UnsupportedResponseException(response, ex);
        ExceptionDispatchInfo.SetCurrentStackTrace(e);
        throw e;
    }

    /// <summary>
    ///     Ensures the execution of a delegate and handles any exceptions by mapping them to a known type.
    ///     If an <see cref="UnsupportedResponseException" /> is caught, it is rethrown.
    ///     Otherwise, any other exceptions are remapped to <see cref="UnsupportedResponseException" />.
    /// </summary>
    /// <typeparam name="T">The return type of the delegate.</typeparam>
    /// <param name="resp">The response string for context.</param>
    /// <param name="del">The delegate to execute.</param>
    /// <returns>The result of the delegate execution.</returns>
    /// <exception cref="UnsupportedResponseException">Thrown when an unsupported response is encountered.</exception>
    internal static T HandleResponse<T>(string resp, Func<T> del)
    {
        try
        {
            return del();
        }
        catch (UnsupportedResponseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MapAndThrowException(resp, ex);
            return default; // Not reachable
        }
    }

    /// <summary>
    ///     Ensures the execution of a delegate and handles any exceptions by mapping them to a known type.
    ///     If an <see cref="UnsupportedResponseException" /> is caught, it is rethrown.
    ///     Otherwise, any other exceptions are remapped to <see cref="UnsupportedResponseException" />.
    /// </summary>
    /// <typeparam name="T">The return type of the delegate.</typeparam>
    /// <param name="resp">The response string for context.</param>
    /// <param name="del">The delegate to execute.</param>
    /// <returns>The result of the delegate execution.</returns>
    /// <exception cref="UnsupportedResponseException">Thrown when an unsupported response is encountered.</exception>
    internal static T HandleResponse<T>(string resp, Func<string, T> del)
    {
        try
        {
            return del(resp);
        }
        catch (UnsupportedResponseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MapAndThrowException(resp, ex);
            return default; // Not reachable
        }
    }

    /// <summary>
    ///     Ensures the execution of a delegate and handles any exceptions by mapping them to a known type.
    ///     If an <see cref="UnsupportedResponseException" /> is caught, it is rethrown.
    ///     Otherwise, any other exceptions are remapped to <see cref="UnsupportedResponseException" />.
    /// </summary>
    /// <param name="resp">The response string for context.</param>
    /// <param name="del">The delegate to execute.</param>
    /// <returns>The result of the delegate execution.</returns>
    /// <exception cref="UnsupportedResponseException">Thrown when an unsupported response is encountered.</exception>
    internal static void HandleResponse(string resp, Action del)
    {
        try
        {
            del();
        }
        catch (UnsupportedResponseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MapAndThrowException(resp, ex);
        }
    }

    /// <summary>
    ///     Ensures the execution of a delegate and handles any exceptions by mapping them to a known type.
    ///     If an <see cref="UnsupportedResponseException" /> is caught, it is rethrown.
    ///     Otherwise, any other exceptions are remapped to <see cref="UnsupportedResponseException" />.
    /// </summary>
    /// <param name="resp">The response string for context.</param>
    /// <param name="del">The delegate to execute.</param>
    /// <returns>The result of the delegate execution.</returns>
    /// <exception cref="UnsupportedResponseException">Thrown when an unsupported response is encountered.</exception>
    internal static void HandleResponse(string resp, Action<string> del)
    {
        try
        {
            del(resp);
        }
        catch (UnsupportedResponseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MapAndThrowException(resp, ex);
        }
    }
}

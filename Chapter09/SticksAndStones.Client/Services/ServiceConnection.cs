using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SticksAndStones.Models;

namespace SticksAndStones.Services;

public class ServiceConnection
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions serializerOptions;

    public AsyncLazy<HubConnection?> Hub;

    private ILogger? Log { get; init; }

    public ServiceConnection(/*ILogger? logger,*/ Settings settings)
    {
        httpClient = new();
        httpClient.BaseAddress ??= new Uri(settings.ServerUrl);
        httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));

        serializerOptions = JsonSerializerOptions.Default;

        //Log = logger;
        Log = null;

        Hub = new(async () =>
        {
            ConnectionInfo? connectionInfo;
            HubConnection? hub = null;

            (connectionInfo, AsyncError? error) = await GetAsync<ConnectionInfo>(new($"{httpClient.BaseAddress}/Lobby/GetConfiguration"), new());

            if (connectionInfo is not null)
            {
                var connectionBuilder = new HubConnectionBuilder();
                connectionBuilder.WithUrl(connectionInfo.Url, (HttpConnectionOptions obj) =>
                {
                    obj.AccessTokenProvider = () => Task.Run(() => connectionInfo.AccessToken);
                });
                hub = connectionBuilder.Build();

                await hub.StartAsync();
            }
            return hub;
        });
    }

    UriBuilder GetUriBuilder(Uri uri, Dictionary<string, string> parameters)
        => new(uri)
        {
            Query = string.Join("&",
            parameters.Select(kvp =>
                 $"{kvp.Key}={kvp.Value}"))
        };

    async ValueTask<AsyncError?> GetError(HttpResponseMessage responseMessage, Stream content)
    {
        AsyncError? error;
        if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
        {
            Log?.LogError("Unauthorized request {@Uri}", responseMessage.RequestMessage?.RequestUri);
            return new()
            {
                Message = "Unauthorized request."
            }; ;
        }

        try
        {
            error = await JsonSerializer.DeserializeAsync<AsyncError>(content, serializerOptions);
        }
        catch (Exception e)
        {
            error = new()
            {
                Message = e.Message,
                //InnerException = e.InnerException?.Message,
            };
        }

        // Log.LogError("{@Error} {@Message} for {@Uri}", error?.ErrorCode, error?.Message,
        // responseMessage?.RequestMessage?.RequestUri);
        return error;
    }

    public async Task<(T? Result, AsyncError? Exception)> GetAsync<T>(Uri uri, Dictionary<string, string> parameters)
    {
        var builder = GetUriBuilder(uri, parameters);
        var fullUri = builder.ToString();
        Log?.LogDebug("{@ObjectType} Get REST call @{RestUrl}", typeof(T).Name, fullUri);
        try
        {
            var responseMessage = await httpClient.GetAsync(fullUri);
            Log?.LogDebug("Response {@ResponseCode} for {@RestUrl}", responseMessage.StatusCode, fullUri);
            if (responseMessage.IsSuccessStatusCode)
            {
                try
                {
                    var content = await responseMessage.Content.ReadFromJsonAsync<T>();
                    Log?.LogDebug("Object of type {@ObjectType} parsed for {@RestUrl}", typeof(T).Name, fullUri);
                    return (content, null);
                }
                catch (Exception e)
                {
                    Log?.LogError("Error {@ErrorMessage} for when parsing ${ObjectType} for {@RestUrl}", e.Message, typeof(T).Name, fullUri);
                    return (default, new AsyncExceptionError()
                    {
                        InnerException = e.InnerException?.Message,
                        Message = e.Message
                    });
                }
            }
            Log?.LogDebug("Returning error for @{RestUrl}", fullUri);
            return (default, await GetError(responseMessage, await responseMessage.Content.ReadAsStreamAsync()));
        }
        catch (Exception e)
        {
            Log?.LogError("Error {@ErrorMessage} for REST call ${ResUrl}", e.Message, fullUri);
            // The service might not be happy with us, we might have connection issues etc..
            return (default, new AsyncExceptionError()
            {
                InnerException = e.InnerException?.Message,
                Message = e.Message
            });
        }
    }

    public async Task<(T? Result, AsyncError? Exception)> PostAsync<T>(Uri uri, object parameter)
    {
        Log?.LogDebug("{@ObjectType} Get REST call @{RestUrl}", typeof(T).Name, uri);
        try
        {
            var responseMessage = await httpClient.PostAsJsonAsync(uri, parameter);
            Log?.LogDebug("Response {@ResponseCode} for {@RestUrl}", responseMessage.StatusCode, uri);
            await using var content = await responseMessage.Content.ReadAsStreamAsync();
            if (responseMessage.IsSuccessStatusCode)
            {
                try
                {
                    Log?.LogDebug("Parse {@ObjectType} SUCCESS for {@RestUrl}", typeof(T).Name, uri);
                    var result = await responseMessage.Content.ReadFromJsonAsync<T>();
                    Log?.LogDebug("Object of type {@ObjectType} parsed for {@RestUrl}", typeof(T).Name, uri);
                    return (result, null);
                }
                catch (Exception e)
                {
                    Log?.LogError("Error {@ErrorMessage} for when parsing ${ObjectType} for {@RestUrl}", e.Message, typeof(T).Name, uri);
                    return (default, new AsyncExceptionError()
                    {
                        InnerException = e.InnerException?.Message,
                        Message = e.Message
                    });
                }
            }
            Log?.LogDebug("Returning error for @{RestUrl}", uri);
            return (default, await GetError(responseMessage, content));
        }
        catch (Exception e)
        {
            Log?.LogError("Error {@ErrorMessage} for REST call ${ResUrl}", e.Message, uri);
            // The service might not be happy with us, we might have connection issues etc..
            return (default, new AsyncExceptionError()
            {
                InnerException = e.InnerException?.Message,
                Message = e.Message
            });
        }
    }

}

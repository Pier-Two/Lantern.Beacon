using System.Net;
using System.Text.Json;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;

namespace Lantern.Beacon.Networking.RestApi;

public class HttpServer(BeaconClientOptions options, ISyncProtocol syncProtocol) : IHttpServer
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    
    public void Start()
    {
        _listener.Prefixes.Add($"http://localhost:{options.HttpPort}/");
        _listener.Start();
        Task.Run(() => HandleRequestsAsync(_cts.Token));
    }
    
    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
    }
    
    private async Task HandleRequestsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var context = await _listener.GetContextAsync();
            await Task.Run(() => ProcessRequestAsync(context), token);
        }
    }
    
    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            if (context.Request.HttpMethod == "GET")
            {
                switch (context.Request.Url.AbsolutePath.ToLower())
                {
                    case "/eth/v1/beacon/light_client/finality_update":
                        await HandleGetRequest<DenebLightClientFinalityUpdate>(context, "LC finality update unavailable");
                        break;
                    case "/eth/v1/beacon/light_client/optimistic_update":
                        await HandleGetRequest<DenebLightClientOptimisticUpdate>(context, "LC optimistic update unavailable");
                        break;
                    default:
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                        break;
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
            }
        }
        catch
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.Close();
        }
    }
    
    private async Task HandleGetRequest<T>(HttpListenerContext context, string notFoundMessage)
    {
        var acceptHeader = context.Request.Headers["Accept"];
        byte[] responseBytes = null;

        if (typeof(T) == typeof(DenebLightClientFinalityUpdate))
        {
            if (!syncProtocol.CurrentLightClientFinalityUpdate.Equals(DenebLightClientFinalityUpdate.CreateDefault()))
            {
                responseBytes = DenebLightClientFinalityUpdate.GetHttpResponse(
                    syncProtocol.CurrentLightClientFinalityUpdate,
                    acceptHeader,
                    options.SyncProtocolOptions.Preset
                );

                context.Response.AddHeader("Eth-Consensus-Version", ForkType.Deneb.ToString().ToLower());
            }
        }
        else if(typeof(T) == typeof(DenebLightClientOptimisticUpdate))
        {
            if (!syncProtocol.CurrentLightClientOptimisticUpdate.Equals(DenebLightClientOptimisticUpdate.CreateDefault()))
            {
                responseBytes = DenebLightClientOptimisticUpdate.GetHttpResponse(
                    syncProtocol.CurrentLightClientOptimisticUpdate, 
                    acceptHeader, 
                    options.SyncProtocolOptions.Preset
                );
            
                context.Response.AddHeader("Eth-Consensus-Version", ForkType.Deneb.ToString().ToLower());
            }
        }
        
        if(responseBytes == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            var error = new { Code = 404, Message = notFoundMessage };
            
            context.Response.ContentType = "application/json";
            
            await context.Response.OutputStream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(error));
            context.Response.Close();
            
            return;
        }

        context.Response.ContentType = acceptHeader.Contains("application/octet-stream") 
            ? "application/octet-stream" 
            : "application/json";
        
        await context.Response.OutputStream.WriteAsync(responseBytes);
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.Close();
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Engine
{
    public class ScriptSystem : GameSystem
    {
        private readonly IServiceProvider services;

        private readonly HashSet<ScriptComponent> registeredScripts = new HashSet<ScriptComponent>();
        private readonly HashSet<ScriptComponent> scriptsToStart = new HashSet<ScriptComponent>();
        private readonly HashSet<SyncScript> syncScripts = new HashSet<SyncScript>();
        private readonly List<ScriptComponent> scriptsToStartCopy = new List<ScriptComponent>();
        private readonly List<SyncScript> syncScriptsCopy = new List<SyncScript>();

        public ScriptSystem(IServiceProvider services)
        {
            this.services = services;
        }

        public override void Update(GameTime gameTime)
        {
            scriptsToStartCopy.AddRange(scriptsToStart);
            scriptsToStart.Clear();

            syncScriptsCopy.AddRange(syncScripts);

            foreach (ScriptComponent script in scriptsToStartCopy)
            {
                if (script is StartupScript startupScript)
                {
                    startupScript.Start();
                }
                else if (script is AsyncScript asyncScript)
                {
                    asyncScript.CancellationTokenSource = new CancellationTokenSource();
                    asyncScript.ExecuteAsync();
                }
            }

            foreach (SyncScript syncScript in syncScriptsCopy)
            {
                syncScript.Update();
            }

            scriptsToStartCopy.Clear();
            syncScriptsCopy.Clear();
        }

        public void Add(ScriptComponent script)
        {
            script.Initialize(services);
            registeredScripts.Add(script);

            scriptsToStart.Add(script);

            if (script is SyncScript syncScript)
            {
                syncScripts.Add(syncScript);
            }
        }

        public void Remove(ScriptComponent script)
        {
            bool startWasPending = scriptsToStart.Remove(script);
            bool wasRegistered = registeredScripts.Remove(script);

            if (!startWasPending && wasRegistered)
            {
                script.Cancel();

                if (script is AsyncScript asyncScript)
                {
                    asyncScript.CancellationTokenSource?.Cancel();
                }
            }

            if (script is SyncScript syncScript)
            {
                syncScripts.Remove(syncScript);
            }
        }
    }
}

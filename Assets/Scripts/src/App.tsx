/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import { motion } from "motion/react";
import { 
  ShieldCheck, 
  Terminal, 
  Settings, 
  GitBranch, 
  RefreshCcw, 
  Box, 
  Layers,
  Code2,
  AlertTriangle,
  History
} from "lucide-react";
import { useState } from "react";

export default function App() {
  const [gitLogs, setGitLogs] = useState([
    { id: 1, action: "INITIALIZED", message: "Unity 2022.3.62f3 (LTS) detected.", time: "10:45 AM" },
    { id: 2, action: "SYNC", message: "Physics Document IBS-PHYS-001 ingested.", time: "11:02 AM" },
    { id: 3, action: "FILE-GEN", message: "IndianBusController.cs created.", time: "11:58 PM" },
    { id: 4, action: "FILE-GEN", message: "IndianEconomyManager.cs created.", time: "12:08 AM" },
    { id: 5, action: "FILE-GEN", message: "IndianBusStop.cs created.", time: "12:09 AM" },
    { id: 6, action: "RESTRICTED", message: "Git Push via Terminal is blocked by system policy.", time: "12:10 AM" },
    { id: 7, action: "MANUAL", message: "Please use 'Download ZIP' to sync with D:\\New project\\Indian Bus Sim", time: "12:10 AM" },
  ]);

  const [activeTab, setActiveTab] = useState("scripts");

  const components = [
    { title: "Economy Mgr", status: "READY", icon: <Box className="w-5 h-5 text-green-500" /> },
    { title: "Stop Logic", status: "READY", icon: <ShieldCheck className="w-5 h-5 text-green-500" /> },
    { title: "Bus Physics", status: "STABLE", icon: <Box className="w-5 h-5 text-green-400" /> },
    { title: "AI Traffic", status: "CREATED", icon: <ShieldCheck className="w-5 h-5 text-green-500" /> },
  ];

  return (
    <div className="min-h-screen bg-[#0a0a0c] text-neutral-300 font-sans selection:bg-orange-500 selection:text-white">
      {/* Header / HUD */}
      <header className="border-b border-white/5 bg-[#0f0f12] px-6 py-4 flex items-center justify-between sticky top-0 z-50 backdrop-blur-md">
        <div className="flex items-center gap-4">
          <div className="w-10 h-10 bg-orange-600 rounded-lg flex items-center justify-center font-black text-white italic">IBS</div>
          <div>
            <h1 className="text-sm font-bold tracking-tight text-white uppercase">Indian Bus Simulator — Dev Lab</h1>
            <p className="text-[10px] font-mono text-neutral-500 tracking-widest flex items-center gap-2">
              <span className="w-1.5 h-1.5 bg-green-500 rounded-full animate-pulse" />
              SYSTEMS: ONLINE // VERSION 1.0.4-BETA
            </p>
          </div>
        </div>
        <div className="flex items-center gap-6 text-[11px] font-mono uppercase tracking-[0.2em] text-neutral-500 hidden md:flex">
          <div className="flex items-center gap-2 border-x border-white/5 px-4 h-8">
            <GitBranch className="w-3 h-3 text-orange-500" />
            <span className="text-neutral-400">Branch: main</span>
          </div>
          <div className="flex items-center gap-2 border-r border-white/5 px-4 h-8 mr-4">
            <RefreshCcw className="w-3 h-3 text-blue-500" />
            <span className="text-neutral-400">Sync: Enabled</span>
          </div>
          <Settings className="w-4 h-4 hover:text-white transition-colors cursor-pointer" />
        </div>
      </header>

      <main className="max-w-[1600px] mx-auto p-6 grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Sidebar: Components */}
        <aside className="lg:col-span-3 space-y-6">
          <div className="bg-[#111114] border border-white/5 rounded-2xl p-5">
            <h2 className="text-xs font-mono text-neutral-500 uppercase tracking-widest mb-4 flex items-center gap-2">
              <Layers className="w-4 h-4" /> Core Modules
            </h2>
            <div className="space-y-3">
              {components.map((comp, idx) => (
                <div key={idx} className="bg-white/5 rounded-xl p-3 flex items-center justify-between group hover:bg-white/10 transition-colors cursor-pointer">
                  <div className="flex items-center gap-3">
                    <div className="p-2 bg-black/40 rounded-lg">{comp.icon}</div>
                    <span className="text-sm font-medium">{comp.title}</span>
                  </div>
                  <span className={`text-[9px] font-mono px-2 py-0.5 rounded-full ${comp.status === "STABLE" ? "bg-green-500/10 text-green-400" : "bg-orange-500/10 text-orange-400"}`}>
                    {comp.status}
                  </span>
                </div>
              ))}
            </div>
          </div>

          <div className="bg-[#111114] border border-white/5 rounded-2xl p-5">
            <h2 className="text-xs font-mono text-neutral-500 uppercase tracking-widest mb-4 flex items-center gap-2">
              <Settings className="w-4 h-4" /> Environment Config
            </h2>
            <div className="grid grid-cols-2 gap-3">
              <div className="bg-white/5 rounded-xl p-4 text-center">
                <p className="text-[10px] text-neutral-500 uppercase mb-1">Time Loop</p>
                <p className="text-xl font-black text-white italic">6X</p>
              </div>
              <div className="bg-white/5 rounded-xl p-4 text-center">
                <p className="text-[10px] text-neutral-500 uppercase mb-1">Region</p>
                <p className="text-xl font-black text-white italic">AP-S1</p>
              </div>
            </div>
          </div>
        </aside>

        {/* Center: File Preview */}
        <main className="lg:col-span-6 space-y-6">
          <div className="bg-[#111114] border border-white/5 rounded-2xl overflow-hidden flex flex-col h-[600px]">
            <div className="bg-[#18181b] px-6 py-3 flex items-center justify-between border-b border-white/5">
              <div className="flex items-center gap-3">
                <Code2 className="w-4 h-4 text-orange-500" />
                <span className="text-sm font-bold text-white tracking-tight uppercase">Script Generation: PotholeSystem.cs</span>
              </div>
              <div className="flex gap-1.5">
                <div className="w-2 h-2 rounded-full bg-red-500/20" />
                <div className="w-2 h-2 rounded-full bg-yellow-500/20" />
                <div className="w-2 h-2 rounded-full bg-green-500/20" />
              </div>
            </div>
            
            <div className="flex-1 overflow-auto p-6 font-mono text-xs leading-relaxed text-neutral-400 bg-black/30">
              <pre className="whitespace-pre-wrap">
                {`// Unity Pothole Suspension Script
using UnityEngine;

public class PotholeSystem : MonoBehaviour 
{
    [Header("Ref: IBS-PHYS-001 Page 11")]
    public float bumpAmplitude = 1.5f;
    public float p_DetectionRadius = 0.5f;

    void OnCollisionEnter(Collision col) {
        if(col.gameObject.CompareTag("Pothole")) {
            ApplySuspensionJolt(col);
        }
    }

    void ApplySuspensionJolt(Collision col) {
        // Impact simulation logic...
        Debug.Log("Pothole Impact Detected.");
    }
}
`}
              </pre>
            </div>

            <div className="bg-[#18181b] p-4 flex items-center justify-between border-t border-white/5 text-[10px] font-mono text-neutral-500 uppercase tracking-widest">
              <span>Path: /Assets/Scripts/IndianBusPotholeSystem.cs</span>
              <span className="flex items-center gap-2">
                <span className="w-1 h-1 bg-green-500 rounded-full" /> UTF-8 // C#
              </span>
            </div>
          </div>
        </main>

        {/* Right Sidebar: Logs */}
        <aside className="lg:col-span-3 space-y-6">
          <div className="bg-[#111114] border border-white/5 rounded-2xl p-5 flex flex-col h-full">
            <h2 className="text-xs font-mono text-neutral-500 uppercase tracking-widest mb-4 flex items-center gap-2">
              <History className="w-4 h-4" /> Live Git Terminal
            </h2>
            <div className="space-y-4 flex-1 overflow-auto pr-2 custom-scrollbar font-mono text-[10px]">
              {gitLogs.map(log => (
                <motion.div 
                  initial={{ opacity: 0, x: -10 }}
                  animate={{ opacity: 1, x: 0 }}
                  key={log.id} 
                  className="border-l-2 border-white/10 pl-4 py-1"
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className={`px-1.5 py-0.5 rounded ${
                      log.action === "COMMIT" ? "bg-blue-500/20 text-blue-400" :
                      log.action === "SYNC" ? "bg-orange-500/20 text-orange-400" :
                      "bg-green-500/20 text-green-400"
                    }`}>
                      {log.action}
                    </span>
                    <span className="text-neutral-600">{log.time}</span>
                  </div>
                  <p className="text-neutral-400 line-clamp-2">{log.message}</p>
                </motion.div>
              ))}
            </div>
            <div className="mt-6 pt-4 border-t border-white/5">
              <div className="bg-black/60 rounded-lg p-3 relative flex items-center gap-3 group border border-white/5">
                <Terminal className="w-3 h-3 text-neutral-600" />
                <input 
                  placeholder="Automated process active..." 
                  disabled
                  className="bg-transparent border-none text-[10px] w-full focus:outline-none italic text-neutral-500"
                />
              </div>
            </div>
          </div>
        </aside>
      </main>

      {/* Global Status Bar */}
      <footer className="fixed bottom-0 w-full bg-[#0f0f12] border-t border-white/5 px-8 py-2.5 flex items-center justify-between text-[9px] font-mono text-neutral-500 uppercase tracking-widest z-50">
        <div className="flex gap-8">
          <span className="flex items-center gap-2"><span className="w-1 h-1 bg-green-500 rounded-full" /> Network: ap-south-1 (Mumbai)</span>
          <span className="flex items-center gap-2"><span className="w-1 h-1 bg-blue-500 rounded-full" /> CDN: CloudFront-Global</span>
        </div>
        <div>
          © 2026 Game Development Studio // IBS-IAC-001
        </div>
      </footer>
    </div>
  );
}



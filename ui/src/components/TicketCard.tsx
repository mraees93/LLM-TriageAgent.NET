import type { SupportTicket } from '../utils/api';

interface TicketCardProps {
  ticket: SupportTicket;
}

export default function TicketCard({ ticket }: TicketCardProps) {
  return (
    <div className="bg-slate-800 border border-slate-700/80 rounded-xl p-5 shadow-sm transition-all hover:border-slate-600">
      
      <div className="flex justify-between items-start gap-4 mb-3">
        <div>
          <h3 className="font-bold text-lg text-slate-100">{ticket.title}</h3>
          <p className="text-xs font-mono text-slate-500 mt-0.5">ID: {ticket.id}</p>
        </div>
        
        {/* State Color Coding */}
        <span className={`px-2.5 py-1 rounded-md text-xs font-bold uppercase tracking-wide border ${
          ticket.status === 'Resolved' ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' :
          ticket.status === 'Processing' ? 'bg-amber-500/10 text-amber-400 border-amber-500/20 animate-pulse' :
          'bg-slate-700/50 text-slate-400 border-slate-600'
        }`}>
          {ticket.status}
        </span>
      </div>

      <p className="text-slate-300 text-sm bg-slate-900/40 rounded-lg p-3 border border-slate-900/50 mb-4">
        {ticket.description}
      </p>

      {/* Autonomous AI Agent Metadata Container (Only renders when analysis begins) */}
      {(ticket.assignedLabel || ticket.agentReply) && (
        <div className="border-t border-slate-700/50 pt-3 mt-3 bg-indigo-950/20 rounded-lg p-3 border border-indigo-900/30 space-y-2">
          
          {/* Autonomous Tag Classification */}
          <div className="flex items-center gap-2">
            <span className="text-xs font-mono font-bold text-indigo-400">🏷️ AI Label Assigned:</span>
            <span className="bg-indigo-500/20 text-indigo-300 text-xs px-2 py-0.5 rounded font-bold uppercase">
              {ticket.assignedLabel || 'Analyzing Layout...'}
            </span>
          </div>
          
          {/* Automated Solution Commentary Output */}
          <div>
            <div className="text-xs font-mono font-bold text-indigo-400 mb-1">💬 AI Agent Reply Resolution:</div>
            <p className="text-sm text-slate-200 italic bg-slate-950/40 p-2.5 rounded border border-slate-900">
              "{ticket.agentReply || 'AI Agent is executing system log tool checks...'}"
            </p>
          </div>

        </div>
      )}

    </div>
  );
}

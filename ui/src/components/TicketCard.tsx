import { type SupportTicket } from '../utils/api';

interface TicketCardProps {
  ticket: SupportTicket;
  onDeleteRequest: (ticket: SupportTicket) => void; // ✅ Pass request up to open your custom modal
}

export default function TicketCard({ ticket, onDeleteRequest }: TicketCardProps) {
  // TELEMETRY LATENCY COMPUTER: Calculates processing speeds in seconds
  const calculateLatency = (startStr: string, endStr: string | null): string => {
    if (!endStr) return '0.0s';
    const start = new Date(startStr);
    const end = new Date(endStr);
    const differenceInSeconds = (end.getTime() - start.getTime()) / 1000;
    return `${Math.max(0, differenceInSeconds).toFixed(1)}s`;
  };

  return (
    <div className="bg-slate-800 border border-slate-700/80 rounded-xl p-5 shadow-sm transition-all hover:border-slate-600 relative">
      
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
          
          {/* Autonomous Tag Classification & Telemetry Timestamps */}
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono font-bold text-indigo-400">AI Label Assigned:</span>
              <span className={`text-xs px-2 py-0.5 rounded font-bold uppercase ${
                ticket.assignedLabel === 'bug' ? 'bg-rose-500/20 text-rose-300' : 'bg-amber-500/20 text-amber-300'
              }`}>
                {ticket.assignedLabel || 'Analyzing Layout...'}
              </span>
            </div>

            {/* ⏱️ Dynamic Telemetry Processing Badge */}
            {ticket.status === 'Resolved' && (
              <span className="bg-indigo-500/20 text-indigo-300 text-xs px-2 py-0.5 rounded font-mono font-bold flex items-center gap-1">
                Latency: {calculateLatency(ticket.createdAt, ticket.resolvedAt)}
              </span>
            )}
          </div>
          
          {/* Automated Solution Commentary Output */}
          <div>
            <div className="text-xs font-mono font-bold text-indigo-400 mb-1">AI Agent Reply Resolution:</div>
            <p className="text-sm text-slate-200 italic bg-slate-950/40 p-2.5 rounded border border-slate-900">
              "{ticket.agentReply || 'AI Agent is executing system log tool checks...'}"
            </p>
          </div>

        </div>
      )}

      {/* 🪓 Reusable Trash Can Delete Control Actions Panel */}
      {(ticket.status === 'Resolved' || ticket.status === 'Failed') && (
        <div className="flex justify-end border-t border-slate-700/30 pt-3 mt-4">
          <button
            onClick={() => onDeleteRequest(ticket)} // ✅ Trigger Custom Confirmation Modal Loop
            className="flex items-center gap-2 text-xs font-medium text-rose-400 hover:text-rose-300 bg-rose-500/10 hover:bg-rose-500/20 border border-rose-500/20 px-3 py-1.5 rounded-lg transition-colors"
          >
            {/* 🔴 SOLID RED VECTOR TRASH CAN ICON */}
            <svg 
              xmlns="http://w3.org" 
              viewBox="0 0 24 24" 
              fill="currentColor" 
              className="w-4 h-4 text-rose-500 flex-shrink-0"
            >
              <path fillRule="evenodd" d="M16.5 4.478v.227a48.816 48.816 0 0 1 3.878.512.75.75 0 1 1-.256 1.478l-.209-.035-1.005 13.07a3 3 0 0 1-2.991 2.77H8.084a3 3 0 0 1-2.991-2.77L4.087 6.66l-.209.035a.75.75 0 0 1-.256-1.478A48.567 48.567 0 0 1 7.5 4.705v-.227c0-1.564 1.213-2.9 2.816-2.951a52.662 52.662 0 0 1 3.369 0c1.603.051 2.815 1.387 2.815 2.951Zm-6.136-1.452a51.196 51.196 0 0 1 3.273 0C14.39 3.05 15 3.684 15 4.478v.113a49.488 49.488 0 0 0-6 0v-.113c0-.794.609-1.428 1.364-1.452Zm-.355 5.945a.75.75 0 1 0-1.5.058l.347 9a.75.75 0 1 0 1.499-.058l-.346-9Zm5.48.058a.75.75 0 1 0-1.498-.058l-.347 9a.75.75 0 0 0 1.5.058l.345-9Z" clipRule="evenodd" />
            </svg>
            <span>Delete ticket</span>
          </button>
        </div>
      )}

    </div>
  );
}

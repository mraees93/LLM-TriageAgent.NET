import type { SupportTicket } from '../utils/api';
import TicketCard from './TicketCard';

interface OperationsMonitorProps {
  tickets: SupportTicket[];
}

export default function OperationsMonitor({ tickets }: OperationsMonitorProps) {
  return (
    <div className="lg:col-span-2 space-y-4">
      <h2 className="text-xl font-bold text-slate-200 flex items-center gap-2">
        📊 Live Agent Operations Monitor
        <span className="animate-pulse inline-block w-2.5 h-2.5 rounded-full bg-green-500"></span>
      </h2>

      {tickets.length === 0 ? (
        // Empty State Banner
        <div className="bg-slate-800/30 border border-dashed border-slate-700 rounded-xl p-12 text-center text-slate-500">
          No tickets processed yet. Submit an issue on the left to wake up the AI agent queue loop!
        </div>
      ) : (
        // Rendered Dynamic Map Stream Array
        <div className="space-y-4 max-h-[70vh] overflow-y-auto pr-2">
          {tickets.map((ticket) => (
            <TicketCard key={ticket.id} ticket={ticket} />
          ))}
        </div>
      )}
    </div>
  );
}

import { useState } from 'react';
import { ticketApi, type SupportTicket } from '../utils/api';
import TicketCard from './TicketCard';
import NotificationModal from './NotificationModal';
import EditTicketModal from './EditTicketModal';

interface OperationsMonitorProps {
  tickets: SupportTicket[];
  onRefresh: () => void; 
}

export default function OperationsMonitor({ tickets, onRefresh }: OperationsMonitorProps) {
  const [selectedDeleteTicket, setSelectedDeleteTicket] = useState<SupportTicket | null>(null);
  const [selectedEditTicket, setSelectedEditTicket] = useState<SupportTicket | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleConfirmedDelete = async () => {
    if (!selectedDeleteTicket) return;
    setIsDeleting(true);
    setErrorMessage(null);
    try {
      await ticketApi.deleteTicketById(selectedDeleteTicket.id);
      onRefresh(); 
      setSelectedDeleteTicket(null); 
    } catch (error) {
      console.error(error);
      setErrorMessage('Network Fault: Could not complete delete transaction loops.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="lg:col-span-2 space-y-4">
      
      {tickets.length === 0 && (
        <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-xl text-xs font-mono text-rose-400 space-y-1.5 animate-pulse mb-2">
          <div className="flex items-center gap-1.5 font-bold text-sm text-rose-300">
            <span>🛑</span>
            <span>Cloud Network Cold-Start Warning</span>
          </div>
          <p className="text-slate-400 leading-normal text-xs">
            Render and Aiven free tiers automatically sleep after inactivity. If the dashboard below is blank on your first load, the cloud server container is currently spinning up. This boot sequence can take up to <span className="text-rose-400 font-bold">50 seconds</span>. Thank you for your patience!
          </p>
        </div>
      )}

      <h2 className="text-xl font-bold text-slate-200 flex items-center gap-2">
        Live Agent Operations Monitor
        <span className="inline-block w-2.5 h-2.5 rounded-full bg-green-500"></span>
      </h2>

      {tickets.length === 0 ? (
        <div className="bg-slate-800/30 border border-dashed border-slate-700 rounded-xl p-12 text-center text-slate-500">
          No tickets processed yet. Submit an issue on the left to wake up the AI agent queue loop!
        </div>
      ) : (
        <div className="space-y-4 max-h-[70vh] overflow-y-auto pr-2">
          {tickets.map((ticket) => (
            <TicketCard 
              key={ticket.id} 
              ticket={ticket} 
              onDeleteRequest={(t) => setSelectedDeleteTicket(t)} 
              onEditRequest={(t) => setSelectedEditTicket(t)}
            />
          ))}
        </div>
      )}

      {selectedEditTicket && (
        <EditTicketModal
          ticket={selectedEditTicket}
          onClose={() => setSelectedEditTicket(null)}
          onUpdateSuccess={onRefresh}
        />
      )}

      {selectedDeleteTicket && !errorMessage && (
        <NotificationModal
          isOpen={true}
          type="error"
          title="Confirm ticket deletion"
          message={`Are you sure you want to permanently delete "${selectedDeleteTicket.title}"? This action cannot be undone.`}
          confirmText="Yes, Delete Record"
          isActionLoading={isDeleting}
          onClose={() => setSelectedDeleteTicket(null)}
          onConfirm={handleConfirmedDelete}
        />
      )}

      {errorMessage && (
        <NotificationModal
          isOpen={true}
          type="error"
          title="Operation Failed"
          message={errorMessage}
          onClose={() => {
            setErrorMessage(null);
            setSelectedDeleteTicket(null);
          }}
        />
      )}
    </div>
  );
}

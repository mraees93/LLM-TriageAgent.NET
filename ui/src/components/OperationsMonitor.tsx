import { useState } from 'react';
import { ticketApi, type SupportTicket } from '../utils/api';
import TicketCard from './TicketCard';
import NotificationModal from './NotificationModal';

interface OperationsMonitorProps {
  tickets: SupportTicket[];
  onRefresh: () => void; // ✅ Added prop to update full-stack view list arrays
}

export default function OperationsMonitor({ tickets, onRefresh }: OperationsMonitorProps) {
  // 🎛️ MULTI-PURPOSE MODAL MANAGEMENT STATE CONTROLLERS
  const [selectedDeleteTicket, setSelectedDeleteTicket] = useState<SupportTicket | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // 🪓 EXECUTE API PURGE LOOP
  const handleConfirmedDelete = async () => {
    if (!selectedDeleteTicket) return;

    setIsDeleting(true);
    setErrorMessage(null);
    try {
      await ticketApi.deleteTicketById(selectedDeleteTicket.id);
      onRefresh(); // Refresh global support ticket grid streams
      setSelectedDeleteTicket(null); // Clear selected item to close dialog cleanly
    } catch (error) {
      console.error('Failed to clear entry from database:', error);
      setErrorMessage('Network Fault: Could not complete delete transaction loops.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="lg:col-span-2 space-y-4">
      <h2 className="text-xl font-bold text-slate-200 flex items-center gap-2">
        Live Agent Operations Monitor
        <span className="inline-block w-2.5 h-2.5 rounded-full bg-green-500"></span>
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
            <TicketCard 
              key={ticket.id} 
              ticket={ticket} 
              onDeleteRequest={(targetTicket) => setSelectedDeleteTicket(targetTicket)} // ✅ Open Modal
            />
          ))}
        </div>
      )}

      {/* 🛡️ REUSED ACTION CONFIRMATION OVERLAY MODAL */}
      {selectedDeleteTicket && !errorMessage && (
        <NotificationModal
          isOpen={true}
          type="error" // Uses red theme color parameters for confirmation items
          title="Confirm ticket deletion"
          message={`Are you sure you want to permanently delete "${selectedDeleteTicket.title}"? This action cannot be undone.`}
          confirmText="Yes, Delete Record"
          isActionLoading={isDeleting}
          onClose={() => setSelectedDeleteTicket(null)}
          onConfirm={handleConfirmedDelete}
        />
      )}

      {/* ⚠️ REUSED TRANSACTION ERROR OVERLAY MODAL */}
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

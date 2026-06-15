import { useState, useEffect, useRef } from 'react';
import { ticketApi } from './utils/api';
import type { SupportTicket } from './utils/api'; 
import TicketForm from './components/TicketForm';
import OperationsMonitor from './components/OperationsMonitor';
import NotificationModal from './components/NotificationModal'; // ✅ Reuses your custom modal

export default function App() {
  const [tickets, setTickets] = useState<SupportTicket[]>([]);
  
  // REAL-TIME SYSTEM FAULT MONITORING STATE CONTROLLERS
  const [activeFaultTicket, setActiveFaultTicket] = useState<SupportTicket | null>(null);
  
  // A persistent memory tracking shield to ensure we only alert ONCE per unique failed ticket ID
  const alertedTicketIds = useRef<Set<string>>(new Set());

  // Real-time operations background polling system
  useEffect(() => {
    // 🛡️ FIRST LOAD SHIELD: Prevents old existing database errors from blocking new pop-ups
    let isFirstLoad = true;

    const unsubscribe = ticketApi.subscribeToTickets((freshData) => {
      setTickets(freshData);

      // On the very first data fetch, log all old failed IDs into memory and exit
      if (isFirstLoad) {
        freshData.forEach((t) => {
          if (t.status === 'Failed') {
            alertedTicketIds.current.add(t.id);
          }
        });
        isFirstLoad = false;
        return;
      }

      // 🔍 LIVE DETECTOR: Intercepts tickets that fail in real-time while watching the screen
      const urgentFault = freshData.find(
        (t) => t.status === 'Failed' && 
               !alertedTicketIds.current.has(t.id)
      );

      if (urgentFault) {
        // Log the ID in our tracking shield so it doesn't pop up infinitely on the next poll check
        alertedTicketIds.current.add(urgentFault.id);
        // TRIGGER CUSTOM MODAL OVERLAY POP-UP INSTANTLY
        setActiveFaultTicket(urgentFault);
      }
    });

    return () => unsubscribe();
  }, []);

  return (
    <div className="min-h-screen bg-slate-900 text-slate-100 p-8 font-sans relative">
      <div className="max-w-6xl mx-auto">
        
        {/* Universal Application Banner Deck */}
        <header className="mb-12 border-b border-slate-800 pb-6 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-extrabold text-indigo-400 flex items-center gap-2">
              LLM-TriageAgent
            </h1>
            <p className="text-slate-400 mt-1">Resilient event-driven support dashboard with background AI triage workers</p>
          </div>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          <TicketForm onTicketSubmitted={() => ticketApi.getAll().then(setTickets)} />

          <OperationsMonitor tickets={tickets} onRefresh={() => ticketApi.getAll().then(setTickets)} />

        </div>
      </div>

      {/* 🚨 REUSED MODAL OVERLAY PANEL: Triggers instantly on background queue exception loops */}
      {activeFaultTicket && (
        <NotificationModal
          isOpen={true}
          type="error"
          title="CRITICAL INFRASTRUCTURE FAULT"
          message={`ALERT: Background queue consumer encountered a critical hardware exception while processing Ticket #${activeFaultTicket.id} ("${activeFaultTicket.title}"). The message has been intercepted and safely quarantined inside the Dead Letter Queue for engineering audit review.`}
          onClose={() => setActiveFaultTicket(null)}
        />
      )}
    </div>
  );
}

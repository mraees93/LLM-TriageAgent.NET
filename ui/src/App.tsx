import { useState, useEffect } from 'react';
import { ticketApi } from './utils/api';
import type { SupportTicket } from './utils/api'; 
import TicketForm from './components/TicketForm';
import OperationsMonitor from './components/OperationsMonitor';

export default function App() {
  const [tickets, setTickets] = useState<SupportTicket[]>([]);

  // Real-time operations background polling system
  useEffect(() => {
    // Connect directly to encapsulated utility subscription stream.
    const unsubscribe = ticketApi.subscribeToTickets((freshData) => {
      setTickets(freshData);
    });

    // Teardown listener quickly when component leaves memory view scope
    return () => unsubscribe();
  }, []);

  return (
    <div className="min-h-screen bg-slate-900 text-slate-100 p-8 font-sans">
      <div className="max-w-6xl mx-auto">
        
        {/* Universal Application Banner Deck */}
        <header className="mb-12 border-b border-slate-800 pb-6 flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-extrabold text-indigo-400 flex items-center gap-2">
              🤖 LLM-TriageAgent.NET
            </h1>
            <p className="text-slate-400 mt-1">Autonomous Event-Driven AI DevOps Support System</p>
          </div>
          <div className="bg-indigo-950/50 text-indigo-400 border border-indigo-800 text-xs px-3 py-1.5 rounded-full font-mono">
            Architecture: Decoupled API-First MVC
          </div>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          <TicketForm onTicketSubmitted={() => ticketApi.getAll().then(setTickets)} />

          <OperationsMonitor tickets={tickets} onRefresh={() => ticketApi.getAll().then(setTickets)} />

        </div>
      </div>
    </div>
  );
}

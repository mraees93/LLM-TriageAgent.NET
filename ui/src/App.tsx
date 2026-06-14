import { useState, useEffect } from 'react';
import { ticketApi } from './utils/api';
import type { SupportTicket } from './utils/api'; // Strict type-only import mapping
import TicketForm from './components/TicketForm';
import OperationsMonitor from './components/OperationsMonitor';

export default function App() {
  const [tickets, setTickets] = useState<SupportTicket[]>([]);

  // Real-time operations background polling system
  useEffect(() => {
    // Connect directly to our encapsulated utility subscription stream.
    // The state update function happens completely safe inside an async callback!
    const unsubscribe = ticketApi.subscribeToTickets((freshData) => {
      setTickets(freshData);
    });

    // Teardown listener instantly when component leaves memory view scope
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

        {/* Core Multi-Column Operational Grid Workspace */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          {/* Component Column 1: The Form Submission Engine */}
          <TicketForm onTicketSubmitted={() => ticketApi.getAll().then(setTickets)} />

          {/* Component Columns 2 & 3: The Live Monitor Operational Deck */}
          <OperationsMonitor tickets={tickets} />

        </div>
      </div>
    </div>
  );
}

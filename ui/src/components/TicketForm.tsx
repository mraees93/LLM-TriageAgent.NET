import React, { useState } from 'react';
import { ticketApi } from '../utils/api';
import NotificationModal from './NotificationModal';

interface TicketFormProps {
  onTicketSubmitted: () => void;
}

export default function TicketForm({ onTicketSubmitted }: TicketFormProps) {
  const [title, setTitle] = useState<string>('');
  const [description, setDescription] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);

  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [modalContent, setModalContent] = useState({ title: '', message: '', type: 'info' as 'error' | 'success' | 'info' });

  const triggerModal = (title: string, message: string, type: 'error' | 'success' | 'info') => {
    setModalContent({ title, message, type });
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    
    if (!title.trim() || !description.trim()) {
      triggerModal("Validation Failed", "Please populate both fields before committing data strings to the broker.", "info");
      return;
    }
    
    setLoading(true);

    const success = await ticketApi.create({ title, description });
    
    if (success) {
      setTitle('');
      setDescription('');
      onTicketSubmitted(); // Trigger parent refresh loop
      triggerModal("Transaction Authorized", "Ticket has been successfully broadcast to the MassTransit event stream.", "success");
    } else {
      triggerModal("Network Fault Detected", "Could not authorize data transaction with the backend server routing framework.", "error");
    }
    setLoading(false);
  };

  return (
    <>
      <div className="bg-slate-800/50 border border-slate-700/60 rounded-xl p-6 h-fit backdrop-blur-sm">
        <h2 className="text-xl font-bold mb-4 text-slate-200">Submit New Support Ticket</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">
              Issue Title
            </label>
            <input
              type="text"
              value={title}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setTitle(e.target.value)}
              placeholder="e.g., Login Crash"
              className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors"
            />
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">
              Problem Description
            </label>
            <textarea
              value={description}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setDescription(e.target.value)}
              rows={4}
              placeholder="Describe the error code or failing page here..."
              className="w-full bg-slate-900 border border-slate-700 rounded-lg px-4 py-2.5 text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors resize-none"
            />
          </div>
          <button
            type="submit"
            disabled={loading} // PROTECTS UX: Instantly locks the button against accidental double-clicks!
            className="w-full bg-indigo-600 hover:bg-indigo-500 text-white font-medium py-2.5 rounded-lg shadow-lg shadow-indigo-600/20 transition-all active:scale-[0.98] disabled:opacity-50"
          >
            {loading ? 'Queueing Ticket...' : 'Publish Ticket to Message Bus 🚀'}
          </button>
        </form>
      </div>

      <NotificationModal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title={modalContent.title}
        message={modalContent.message}
        type={modalContent.type}
      />
    </>
  );
}

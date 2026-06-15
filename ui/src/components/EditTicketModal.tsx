import { useState } from 'react';
import { ticketApi, type SupportTicket } from '../utils/api';

interface EditTicketModalProps {
  ticket: SupportTicket;
  onClose: () => void;
  onUpdateSuccess: () => void;
}

export default function EditTicketModal({ ticket, onClose, onUpdateSuccess }: EditTicketModalProps) {
  const [title, setTitle] = useState(ticket.title);
  const [description, setDescription] = useState(ticket.description);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim()) return;

    setIsSubmitting(true);
    setError(null);

    const success = await ticketApi.update(ticket.id, { title, description });
    if (success) {
      onUpdateSuccess();
      onClose();
    } else {
      setError('Network Fault: Could not complete update transaction.');
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/70 backdrop-blur-sm">
      <div className="w-full max-w-lg border border-slate-700 bg-slate-800 rounded-xl p-6 shadow-2xl animate-in fade-in zoom-in-95 duration-150">
        
        <div className="flex justify-between items-start mb-4 border-b border-slate-700 pb-3">
          <div>
            <h3 className="text-lg font-bold text-slate-100 flex items-center gap-2">
              Edit Ticket
            </h3>
            {/* 🆔 ADDED TICKET ID TRACKER SUBTITLE */}
            <p className="text-xs font-mono text-slate-500 mt-1">
              Ticket ID: <span className="text-slate-400">{ticket.id}</span>
            </p>
          </div>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-200 font-mono text-sm">
            ✕ Close
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-indigo-400 uppercase tracking-wider mb-1">
              Issue Title
            </label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              disabled={isSubmitting}
              className="w-full bg-slate-900 border border-slate-700 rounded-lg p-2.5 text-slate-100 focus:outline-none focus:border-indigo-500 text-sm"
              required
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-indigo-400 uppercase tracking-wider mb-1">
              Problem Description
            </label>
            <textarea
              rows={4}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              disabled={isSubmitting}
              className="w-full bg-slate-900 border border-slate-700 rounded-lg p-2.5 text-slate-100 focus:outline-none focus:border-indigo-500 text-sm"
              required
            />
          </div>

          {error && (
            <div className="p-3 bg-rose-500/10 border border-rose-500/20 rounded-lg text-xs font-mono text-rose-400">
              ⚠️ {error}
            </div>
          )}

          <div className="flex justify-end gap-3 text-sm font-medium border-t border-slate-700 pt-3 mt-2">
            <button
              type="button"
              disabled={isSubmitting}
              onClick={onClose}
              className="bg-slate-700 hover:bg-slate-600 border border-slate-600 text-slate-200 px-4 py-2 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2 rounded-lg transition-colors shadow-md"
            >
              {isSubmitting ? 'Saving...' : 'Save and Re-Triage'}
            </button>
          </div>
        </form>

      </div>
    </div>
  );
}

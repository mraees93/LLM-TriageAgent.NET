interface NotificationModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  message: string;
  type: 'error' | 'success' | 'info';
}

export default function NotificationModal({ isOpen, onClose, title, message, type }: NotificationModalProps) {
  if (!isOpen) return null;

  const typeStyles = {
    error: 'border-rose-500/30 bg-rose-950/20 text-rose-400 button:bg-rose-600 hover:button:bg-rose-500',
    success: 'border-emerald-500/30 bg-emerald-950/20 text-emerald-400 button:bg-emerald-600 hover:button:bg-emerald-500',
    info: 'border-indigo-500/30 bg-indigo-950/20 text-indigo-400 button:bg-indigo-600 hover:button:bg-indigo-500'
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-fade-in">
      <div className={`w-full max-w-md border rounded-xl p-6 shadow-2xl backdrop-blur-md ${typeStyles[type]} border-slate-700 bg-slate-800`}>
        
        {/* Header Icon Indicator */}
        <div className="flex items-center gap-3 mb-3">
          <span className="text-xl">
            {type === 'error' ? '❌' : type === 'success' ? '✅' : 'ℹ️'}
          </span>
          <h3 className="text-lg font-bold text-slate-100">{title}</h3>
        </div>

        {/* Content Body */}
        <p className="text-sm text-slate-300 mb-6 leading-relaxed">
          {message}
        </p>

        {/* Footer Dismiss Button */}
        <div className="flex justify-end">
          <button
            onClick={onClose}
            className={`px-4 py-2 text-sm font-medium text-white rounded-lg transition-colors shadow-md ${
              type === 'error' ? 'bg-rose-600 hover:bg-rose-500' :
              type === 'success' ? 'bg-emerald-600 hover:bg-emerald-500' :
              'bg-indigo-600 hover:bg-indigo-500'
            }`}
          >
            Dismiss Operational Log
          </button>
        </div>

      </div>
    </div>
  );
}

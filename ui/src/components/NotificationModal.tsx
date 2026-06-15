interface NotificationModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  message: string;
  type: 'error' | 'success' | 'info';
  onConfirm?: () => void;      
  confirmText?: string;       
  isActionLoading?: boolean;  
}

export default function NotificationModal({ 
  isOpen, 
  onClose, 
  title, 
  message, 
  type,
  onConfirm,
  confirmText = "Confirm Action",
  isActionLoading = false
}: NotificationModalProps) {
  if (!isOpen) return null;

  const typeStyles = {
    error: 'border-rose-500/30 bg-rose-950/20 text-rose-400',
    success: 'border-emerald-500/30 bg-emerald-950/20 text-emerald-400',
    info: 'border-indigo-500/30 bg-indigo-950/20 text-indigo-400'
  };

  const isInteractiveConfirm = !!onConfirm;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm">
      <div className={`w-full max-w-md border rounded-xl p-6 shadow-2xl backdrop-blur-md ${typeStyles[type]} border-slate-700 bg-slate-800 animate-in fade-in zoom-in-95 duration-150`}>
        
        <div className="flex items-center gap-3 mb-3">
          <span className="text-xl">
            {type === 'error' ? '❌' : type === 'success' ? '✅' : 'ℹ️'}
          </span>
          <h3 className="text-lg font-bold text-slate-100">{title}</h3>
        </div>

        <p className="text-sm text-slate-300 mb-6 leading-relaxed">
          {message}
        </p>

        <div className="flex justify-end gap-3 text-sm font-medium">
          {isInteractiveConfirm ? (
            <>
              <button
                disabled={isActionLoading}
                onClick={onClose}
                className="bg-slate-700 hover:bg-slate-600 border border-slate-600 text-slate-200 px-4 py-2 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                disabled={isActionLoading}
                onClick={onConfirm}
                className={`px-4 py-2 text-white rounded-lg transition-colors shadow-md ${
                  type === 'error' ? 'bg-rose-600 hover:bg-rose-500 shadow-rose-950/20' :
                  type === 'success' ? 'bg-emerald-600 hover:bg-emerald-500 shadow-emerald-950/20' :
                  'bg-indigo-600 hover:bg-indigo-500 shadow-indigo-950/20'
                }`}
              >
                {isActionLoading ? 'Processing...' : confirmText}
              </button>
            </>
          ) : (
            <button
              onClick={onClose}
              className={`px-4 py-2 text-sm font-medium text-white rounded-lg transition-colors shadow-md ${
                type === 'error' ? 'bg-rose-600 hover:bg-rose-500' :
                type === 'success' ? 'bg-emerald-600 hover:bg-emerald-500' :
                'bg-indigo-600 hover:bg-indigo-500'
              }`}
            >
              Close
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

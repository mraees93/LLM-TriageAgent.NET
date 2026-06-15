export interface SupportTicket {
  id: string;
  title: string;
  description: string;
  status: string;
  assignedLabel: string | null;
  agentReply: string | null;
  createdAt: string;
}

export interface CreateTicketDto {
  title: string;
  description: string;
}

// ✅ FIXED: Matches your working URL shortener naming layout exactly!
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5088/api/tickets';

export const ticketApi = {
  // GET: Fetch all tickets
  getAll: async (): Promise<SupportTicket[]> => {
    try {
      const response = await fetch(`${API_BASE_URL}`);
      if (!response.ok) throw new Error('Network response was not ok');
      return await response.json();
    } catch (error) {
      console.error('Error fetching tickets:', error);
      return [];
    }
  },

  // POST: Publish a new ticket
  create: async (dto: CreateTicketDto): Promise<boolean> => {
    try {
      const response = await fetch(`${API_BASE_URL}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto),
      });
      return response.ok;
    } catch (error) {
      console.error('Error creating ticket:', error);
      return false;
    }
  },

  // ✅ ADDED: Delete a ticket off the user interface grid (Matches deleteUrlByID!)
  deleteTicketById: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw new Error(`Failed to delete item: ${response.statusText}`);
    }
    console.log('Ticket record deleted successfully from cloud table context.');
  },

  // Encapsulated Polling Subscription Loop
  subscribeToTickets: (callback: (tickets: SupportTicket[]) => void, intervalMs = 3000): () => void => {
    ticketApi.getAll().then(callback);

    const interval = setInterval(async () => {
      const freshData = await ticketApi.getAll();
      callback(freshData);
    }, intervalMs);

    return () => clearInterval(interval);
  }
};

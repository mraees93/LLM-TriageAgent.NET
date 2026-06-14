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

// Keep your existing Vercel variable name! 
// This reads the raw root url (https://onrender.com)
const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:5088';

export const ticketApi = {
  // GET: Fetch all tickets (Appends the route path cleanly just like your working app!)
  getAll: async (): Promise<SupportTicket[]> => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/tickets`);
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
      const response = await fetch(`${API_BASE_URL}/api/tickets`, {
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

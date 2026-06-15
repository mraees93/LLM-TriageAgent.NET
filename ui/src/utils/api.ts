export interface SupportTicket {
  id: string;
  title: string;
  description: string;
  status: string;
  assignedLabel: string | null;
  agentReply: string | null;
  createdAt: string;
  resolvedAt: string | null;
}

export interface CreateTicketDto {
  title: string;
  description: string;
}

// Keep your existing Vercel variable name exactly as it is!
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5088';

export const ticketApi = {
  // GET: Fetch all tickets (Explicitly appends the controller route!)
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

  // DELETE: Clear a ticket off the dashboard layout
  deleteTicketById: async (id: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw new Error(`Failed to delete item: ${response.statusText}`);
    }
    console.log('Ticket record deleted successfully.');
  },

    // PUT: Update an existing ticket text payload matching its original ID
  update: async (id: string, dto: CreateTicketDto): Promise<boolean> => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/tickets/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto),
      });
      return response.ok;
    } catch (error) {
      console.error('Error updating ticket record context:', error);
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

// Strict TypeScript structure mapping to your C# SupportTicket Model
export interface SupportTicket {
  id: string;
  title: string;
  description: string;
  status: string;
  assignedLabel: string | null;
  agentReply: string | null;
  createdAt: string;
}

// Simple structure for data entering the form
export interface CreateTicketDto {
  title: string;
  description: string;
}

const BASE_URL = 'http://localhost:5088/api/tickets';

export const ticketApi = {
  // GET: Fetch all tickets from the database context
  getAll: async (): Promise<SupportTicket[]> => {
    try {
      const response = await fetch(BASE_URL);
      if (!response.ok) throw new Error('Network response was not ok');
      return await response.json();
    } catch (error) {
      console.error('Error fetching tickets:', error);
      return [];
    }
  },

  // POST: Publish a new ticket to the MassTransit message queue controller
  create: async (dto: CreateTicketDto): Promise<boolean> => {
    try {
      const response = await fetch(BASE_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto),
      });
      return response.ok;
    } catch (error) {
      console.error('Error creating ticket:', error);
      return false;
    }
  }
};

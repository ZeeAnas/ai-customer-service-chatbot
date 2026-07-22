# Montana Barber AI Chatbot

A full-stack AI customer-service chatbot built for Montana Barber Shop.

The application uses a Next.js frontend and an **ASP**.**NET** Core Web **API** backend. Customer questions are sent from the frontend to the backend, which securely communicates with the OpenAI Responses **API** and returns an AI-generated response based on Montana Barber Shop’s business information.

Overview

The goal of this project is to create an AI-powered customer-service assistant that can answer common questions about Montana Barber Shop.

The chatbot can provide information about:

- Haircuts and barber services
- Prices
- Estimated service durations
- Contact details
- Booking guidance
- General customer-service questions

The chatbot is designed to answer briefly, politely and in the same language as the customer whenever possible.

### Current Functionality

- AI-powered customer-service responses
- Montana Barber Shop service and price information
- English and Norwegian question support
- Responsive chat interface
- **ASP**.**NET** Core **REST** **API**
- OpenAI Responses **API** integration
- Secure **API**-key management
- Request validation
- Global exception handling
- Structured backend logging
- Loading states in the frontend
- User-friendly error messages
- Request timeout handling
- Request cancellation support
- Separate frontend and backend service layers

### Technology Stack

Backend

- C#
- .**NET**
- **ASP**.**NET** Core Web **API**
- OpenAI Responses **API**
- HttpClient
- Swagger / OpenAPI
- **ASP**.**NET** Core Dependency Injection
- **ASP**.**NET** Core User Secrets
- Global exception middleware
- Structured logging with ILogger

Frontend

- Next.js
- React
- TypeScript
- Tailwind **CSS**
- Fetch **API**

### Project Structure

.
├── backend/
│   └── Chatbot.Api/
│       ├── Configuration/
│       ├── Controllers/
│       ├── Exceptions/
│       ├── Interfaces/
│       ├── Middleware/
│       ├── Models/
│       │   ├── Requests/
│       │   └── Responses/
│       ├── Services/
│       ├── Program.cs
│       └── appsettings.json
│
└── frontend/
    └── chatbot-ui/
    ├── public/
    ├── src/
    │   ├── app/
    │   ├── services/
    │   └── types/
    └── package.json

Architecture

The project follows a separated frontend and backend architecture.

Customer ↓ Next.js Chat Interface ↓ ### Chat Service ↓ **ASP**.**NET** Core **API** ↓ ChatController ↓ IChatService ↓ ChatService ↓ OpenAI Responses **API**

The frontend is responsible for:

- Displaying the chat interface
- Managing user input
- Displaying loading states
- Displaying chatbot responses
- Displaying errors returned by the **API**

The backend is responsible for:

- Validating requests
- Protecting the OpenAI **API** key
- Sending requests to OpenAI
- Providing Montana Barber Shop business instructions
- Parsing OpenAI responses
- Logging errors
- Returning safe **API** responses

### Backend Structure

Controllers

The controller receives **HTTP** requests from the frontend.

Controllers/ChatController.cs

Responsibilities:

- Receive chat messages
- Validate the request
- Call the chat service
- Return the chatbot response

Interfaces

The service interface defines the contract used by the controller.

Interfaces/IChatService.cs

This makes the application easier to maintain, test and extend.

Services

The chat service contains the main OpenAI integration logic.

Services/ChatService.cs

Responsibilities:

- Create the OpenAI request
- Include the chatbot instructions
- Send the customer message
- Receive the OpenAI response
- Extract the generated reply
- Log successful and failed requests

Configuration

OpenAI settings are mapped to a strongly typed configuration class.

Configuration/OpenAiOptions.cs

The configuration contains:

- OpenAI base **URL**
- OpenAI model
- OpenAI **API** key

The **API** key is not stored directly in the source code.

Middleware

The project uses global exception middleware.

Middleware/GlobalExceptionMiddleware.cs

The middleware catches errors and converts them into safe **JSON** responses.

This avoids exposing internal application details to customers.

Exceptions

OpenAI-specific failures use a custom exception.

Exceptions/OpenAiServiceException.cs

This helps distinguish external OpenAI failures from unexpected application errors.

Models

The **API** uses request and response models.

Models/
├── Requests/
│   └── ChatRequest.cs
└── Responses/
    ├── ChatResponse.cs
    └── ErrorResponse.cs

**API** Endpoint

Send a Chat Message

**POST** /api/chat

### Request Body

{ *message*: *How much does a haircut cost?* }

### Successful Response

{ *reply*: *A men's haircut costs **500** **NOK**.* }

### Empty Message Response

{ *error*: *Message is required.* }

### Message Too Long Response

{ *error*: *Message cannot be longer than **1000** characters.* }

AI Service Error Response

{
    *error*: *The AI service is temporarily unavailable.*,
    *traceId*: *request-trace-id*
}

### Request Validation

The backend currently validates that:

- The message is not empty
- The message does not contain only whitespace
- The message is no longer than 1,**000** characters

Validation is performed on the backend even though the frontend also limits message length.

This prevents invalid requests from bypassing frontend validation.

OpenAI Integration

The backend communicates with the OpenAI Responses **API**.

Each request contains:

- The selected OpenAI model
- The Montana Barber Shop system instructions
- The customer’s message

The chatbot instructions include verified business information such as:

- Services
- Prices
- Estimated service durations
- Address
- Contact information
- Booking guidance
- Response rules

The AI is instructed not to invent information that is not included in the business instructions.

Security

The OpenAI **API** key is never exposed to the frontend.

The frontend communicates only with the **ASP**.**NET** Core backend.

Frontend ↓ Backend ↓ OpenAI

The **API** key is stored securely using **ASP**.**NET** Core User Secrets during local development.

The frontend only stores the public backend **URL**.

Sensitive information is not written to application logs.

The backend does not log:

- The OpenAI **API** key
- Full customer messages
- Full OpenAI responses

### Error Handling

The backend uses centralized global exception handling.

Errors are converted into safe responses depending on the situation.

Situation	**HTTP** Status	Response
Empty message	**400**	Message is required
Message too long	**400**	Message cannot exceed 1,**000** characters
OpenAI unavailable	**503**	AI service is temporarily unavailable
OpenAI timeout	**504**	AI service took too long to respond
Unexpected backend error	**500**	Unexpected server error
Backend unreachable	No response	Frontend displays connection error

Detailed technical errors are written to the backend logs.

Customers only receive safe and understandable messages.

Logging

The backend uses ILogger for structured logging.

The application logs:

- OpenAI request attempts
- Successful OpenAI responses
- OpenAI service failures
- **HTTP** status codes
- Request cancellations
- Unexpected backend errors
- Request trace identifiers

Structured logging makes it easier to identify and troubleshoot failed requests.

### Request Cancellation

The backend supports CancellationToken.

If the customer closes the browser, leaves the page or cancels the request, **ASP**.**NET** Core can stop processing the request.

The cancellation token is passed through:

ChatController ↓ IChatService ↓ ChatService ↓ HttpClient

This avoids unnecessary work and unnecessary OpenAI **API** usage.

### Timeout Handling

The OpenAI **HTTP** client has a configured timeout.

If OpenAI does not respond within the allowed time, the request is cancelled and the frontend receives a user-friendly timeout message.

This prevents requests from waiting indefinitely.

### Chatbot Behaviour

The chatbot is instructed to:

- Act as the customer-service assistant for Montana Barber Shop
- Answer clearly and politely
- Keep answers concise
- Use the same language as the customer when possible
- Use **NOK** when mentioning prices
- Use only the provided business information
- Avoid inventing unknown information
- Recommend contacting the barbershop when information is unavailable
- Avoid claiming that it completed a booking
- Avoid claiming that it checked live appointment availability

### Example Questions

Customers can ask questions such as:

How much does a men's haircut cost? Hvor mye koster studentklipp? How long does a skin fade take? Can I book an appointment online? What is the phone number? Do you offer beard colouring?

### Current Limitations

- Conversation history is not yet supported
- Each message is currently treated as an independent request
- The chatbot cannot access live appointment availability
- The chatbot cannot create bookings
- The chatbot cannot update bookings
- The chatbot cannot cancel bookings
- Business information is currently stored inside the system prompt
- Website changes are not synchronized automatically
- There is no database
- There is no authentication
- There is no admin dashboard
- There is no Retrieval-Augmented Generation
- There is no vector database
- Chat conversations are not stored permanently

### Planned Improvements

Future improvements may include:

- Multi-turn conversation history
- Conversation identifiers
- Improved chatbot interface
- Automatic message scrolling
- Clear conversation button
- Suggested customer questions
- Database persistence
- Stored conversations
- Retrieval-Augmented Generation
- Automatic website content retrieval
- Vector database integration
- Admin dashboard
- Editable business information
- Live booking-system integration
- Authentication and authorization
- Automated backend tests
- Frontend component tests
- Rate limiting
- Production deployment
- Monitoring and analytics

### Project Status

Milestone 1 is complete.

The application currently includes:

- A working Next.js chat interface
- A working **ASP**.**NET** Core **API**
- OpenAI integration
- Montana Barber Shop business knowledge
- Request validation
- Global error handling
- Logging
- Request cancellation
- Timeout handling
- Secure **API**-key management

The next planned milestone is multi-turn conversation history, allowing the chatbot to understand previous messages in the same conversation.

Disclaimer

This project is an educational portfolio application.

Business information, services and prices should be reviewed and kept up to date before the application is used in production.

The chatbot does not currently have access to live booking availability or Montana Barber Shop’s internal systems.
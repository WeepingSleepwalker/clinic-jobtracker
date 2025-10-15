```mermaid
graph LR
  A[Client] --> B[API Gateway]
  B --> C[API Server]
  C --> D[(Database)]
 graph LR
  %% LAYOUT
  %% LR = left-to-right, TB = top-to-bottom
  classDef box fill:#fff,stroke:#333,stroke-width:1px,rx:8,ry:8;

  subgraph Client
    A[Web / Mobile Client]:::box
  end

  subgraph Edge
    B[API Gateway / Load Balancer]:::box
  end

  subgraph API["API Service"]
    C[Auth Middleware (JWT/OAuth2)]:::box
    D[Controllers / Route Handlers]:::box
    E[Service Layer (Business Logic)]:::box
    F[Repository / Data Access]:::box
  end

  subgraph Infra["Infrastructure"]
    G[(Primary DB)]:::box
    H[(Cache: Redis)]:::box
    I[[Message Queue]]:::box
    J[[External Service / 3rd-Party API]]:::box
    K[(Object Storage / CDN)]:::box
    L[Observability (Logs/Metrics/Tracing)]:::box
  end

  %% FLOWS
  A --> B --> C --> D --> E --> F --> G
  E <-->|read-through| H
  E --> I
  E --> J
  D --> K
  E --> L

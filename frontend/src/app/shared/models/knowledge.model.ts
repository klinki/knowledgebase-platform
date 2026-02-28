export interface KnowledgeItem {
  id: string;
  title: string;
  url: string;
  capturedAt: Date;
}

export interface Tag {
  id: string;
  name: string;
  count: number;
  lastUsed: Date;
}

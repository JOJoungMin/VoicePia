// 작성 화면(폴더 + 글)의 데이터 구조와 API 호출
//
// 서버(DB)에는 폴더(folders)와 글(documents)이 따로 저장되고, 번호도 따로 매겨진다.
// 그래서 화면에서는 둘을 하나의 트리로 그리기 위해 "f:3"(폴더 3번), "d:3"(글 3번)처럼
// 종류를 앞에 붙인 키로 구분한다.

import { apiFetch } from "./api";

// ---- 서버가 주는 모양 (C# Model과 같음) ----

export type FolderInfo = {
  folderId: number;
  parentFolderId: number | null;
  name: string;
  sortOrder: number;
};

export type DocumentSummary = {
  documentId: number;
  folderId: number | null;
  title: string;
  sortOrder: number;
  contentLength: number;
  updatedAt: string;
};

export type DocumentDetail = DocumentSummary & { content: string };

type WorkspaceTree = { folders: FolderInfo[]; documents: DocumentSummary[] };

// ---- 화면에서 쓰는 트리 노드 ----

export type NodeType = "folder" | "doc";

export type TreeNode = {
  id: string;              // "f:3" 또는 "d:3"
  parentId: string | null; // 부모 폴더의 키 ("f:1"), 맨 위면 null
  type: NodeType;
  name: string;
  order: number;
  dbId: number;            // 서버의 folderId 또는 documentId
};

export const folderKey = (id: number) => `f:${id}`;
export const docKey = (id: number) => `d:${id}`;

export function toTreeNodes(folders: FolderInfo[], docs: DocumentSummary[]): TreeNode[] {
  return [
    ...folders.map((f) => ({
      id: folderKey(f.folderId),
      parentId: f.parentFolderId == null ? null : folderKey(f.parentFolderId),
      type: "folder" as const,
      name: f.name,
      order: f.sortOrder,
      dbId: f.folderId,
    })),
    ...docs.map((d) => ({
      id: docKey(d.documentId),
      parentId: d.folderId == null ? null : folderKey(d.folderId),
      type: "doc" as const,
      name: d.title,
      order: d.sortOrder,
      dbId: d.documentId,
    })),
  ];
}

// ---- API 호출 ----

export const workspaceApi = {
  tree: () => apiFetch<WorkspaceTree>("/api/workspace/tree"),

  createFolder: (parentFolderId: number | null, name: string) =>
    apiFetch<FolderInfo>("/api/workspace/folders", { method: "POST", body: { parentFolderId, name } }),
  renameFolder: (folderId: number, name: string) =>
    apiFetch<boolean>(`/api/workspace/folders/${folderId}`, { method: "PATCH", body: { name } }),
  deleteFolder: (folderId: number) =>
    apiFetch<boolean>(`/api/workspace/folders/${folderId}`, { method: "DELETE" }),

  createDocument: (folderId: number | null, title: string) =>
    apiFetch<DocumentDetail>("/api/workspace/documents", { method: "POST", body: { folderId, title } }),
  getDocument: (documentId: number) =>
    apiFetch<DocumentDetail>(`/api/workspace/documents/${documentId}`),
  updateDocument: (documentId: number, body: { title?: string; content?: string }) =>
    apiFetch<DocumentDetail>(`/api/workspace/documents/${documentId}`, { method: "PATCH", body }),
  deleteDocument: (documentId: number) =>
    apiFetch<boolean>(`/api/workspace/documents/${documentId}`, { method: "DELETE" }),
};

// ---- 트리 계산 ----

// 한 폴더 바로 아래의 항목들: 폴더 먼저, 그다음 만든 순서
export function childrenOf(nodes: TreeNode[], parentId: string | null): TreeNode[] {
  return nodes
    .filter((n) => n.parentId === parentId)
    .sort((a, b) => {
      if (a.type !== b.type) return a.type === "folder" ? -1 : 1;
      return a.order - b.order;
    });
}

// 어떤 항목과 그 아래 모든 항목의 키 (화면에서 지울 때 사용. DB는 CASCADE로 알아서 지운다)
export function descendantIds(nodes: TreeNode[], id: string): Set<string> {
  const result = new Set<string>([id]);
  let added = true;
  while (added) {
    added = false;
    for (const n of nodes) {
      if (n.parentId && result.has(n.parentId) && !result.has(n.id)) {
        result.add(n.id);
        added = true;
      }
    }
  }
  return result;
}

// 맨 위부터 이 항목까지의 경로 (예: 내 작품 › 1부 › 3화)
export function pathOf(nodes: TreeNode[], id: string): TreeNode[] {
  const byId = new Map(nodes.map((n) => [n.id, n]));
  const path: TreeNode[] = [];
  let current = byId.get(id);
  while (current) {
    path.unshift(current);
    current = current.parentId ? byId.get(current.parentId) : undefined;
  }
  return path;
}

// 같은 폴더 안에서 이름이 겹치지 않게: "새 글", "새 글 2", "새 글 3" ...
export function uniqueName(nodes: TreeNode[], parentId: string | null, type: NodeType): string {
  const base = type === "folder" ? "새 폴더" : "새 글";
  const names = new Set(nodes.filter((n) => n.parentId === parentId && n.type === type).map((n) => n.name));
  if (!names.has(base)) return base;
  let i = 2;
  while (names.has(`${base} ${i}`)) i++;
  return `${base} ${i}`;
}

// ---- 글자 수 (웹소설은 보통 "공백 포함" 글자 수로 분량을 잰다) ----

export function countChars(text: string) {
  const withSpaces = [...text.replace(/\r?\n/g, "")].length;
  const withoutSpaces = [...text.replace(/\s/g, "")].length;
  return { withSpaces, withoutSpaces };
}

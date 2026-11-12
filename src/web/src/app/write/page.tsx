"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { apiFetch, type UserInfo } from "@/lib/api";
import {
  childrenOf,
  descendantIds,
  docKey,
  folderKey,
  pathOf,
  toTreeNodes,
  uniqueName,
  workspaceApi,
  type NodeType,
  type TreeNode,
} from "@/lib/workspace";
import Editor, { type SaveState } from "./Editor";
import FileTree from "./FileTree";

// 지금 열려 있는 글
type OpenDoc = { key: string; dbId: number; content: string | null };

// 본문은 타이핑을 멈추고 이만큼 지나면 서버에 저장한다
const SAVE_DELAY_MS = 800;

export default function WritePage() {
  const router = useRouter();
  const [user, setUser] = useState<UserInfo | null>(null);
  const [nodes, setNodes] = useState<TreeNode[] | null>(null); // null = 불러오는 중
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [renamingId, setRenamingId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [openDoc, setOpenDoc] = useState<OpenDoc | null>(null);
  const [saveState, setSaveState] = useState<SaveState>("saved");
  const [error, setError] = useState("");

  // 아직 서버로 보내지 않은 본문 (타이핑 중)
  const pending = useRef<{ dbId: number; content: string } | null>(null);
  const saveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  // 1. 로그인 확인 → 탐색기 불러오기
  useEffect(() => {
    (async () => {
      const me = await apiFetch<UserInfo>("/api/auth/me");
      if (!me.success || !me.data) {
        router.replace("/login");
        return;
      }
      setUser(me.data);
      const tree = await workspaceApi.tree();
      if (!tree.success || !tree.data) {
        setError(tree.message);
        setNodes([]);
        return;
      }
      setNodes(toTreeNodes(tree.data.folders, tree.data.documents));
    })();
  }, [router]);

  // 2. 본문 저장: 쌓여 있는 변경을 지금 바로 보낸다
  const flush = useCallback(async () => {
    if (saveTimer.current) clearTimeout(saveTimer.current);
    const job = pending.current;
    if (!job) return;
    pending.current = null;
    const result = await workspaceApi.updateDocument(job.dbId, { content: job.content });
    setSaveState(result.success ? "saved" : "error");
  }, []);

  // 창을 닫을 때 저장 못 한 본문이 있으면 마지막으로 한 번 보낸다
  useEffect(() => {
    const onLeave = () => {
      const job = pending.current;
      if (!job) return;
      fetch(`/api/workspace/documents/${job.dbId}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ content: job.content }),
        keepalive: true, // 페이지가 닫혀도 요청은 끝까지 보낸다
      });
    };
    window.addEventListener("beforeunload", onLeave);
    return () => window.removeEventListener("beforeunload", onLeave);
  }, []);

  const list = nodes ?? [];
  const selected = list.find((n) => n.id === selectedId) ?? null;

  // 3. 글을 고르면: 이전 글의 남은 변경을 저장하고, 새 글의 본문을 서버에서 불러온다
  async function select(id: string) {
    setSelectedId(id);
    const node = list.find((n) => n.id === id);
    if (!node || node.type !== "doc" || openDoc?.key === id) return;

    await flush();
    setOpenDoc({ key: id, dbId: node.dbId, content: null });
    const result = await workspaceApi.getDocument(node.dbId);
    setOpenDoc((cur) => (cur?.key === id ? { ...cur, content: result.success && result.data ? result.data.content : "" } : cur));
    if (!result.success) setError(result.message);
  }

  function handleContent(content: string) {
    if (!openDoc) return;
    setOpenDoc({ ...openDoc, content });
    pending.current = { dbId: openDoc.dbId, content };
    setSaveState("saving");
    if (saveTimer.current) clearTimeout(saveTimer.current);
    saveTimer.current = setTimeout(flush, SAVE_DELAY_MS);
  }

  // 새 항목은 "선택한 폴더 안" 또는 "선택한 글과 같은 폴더"에 만든다 (VS Code와 같은 규칙)
  function targetFolder(): TreeNode | null {
    if (!selected) return null;
    if (selected.type === "folder") return selected;
    return list.find((n) => n.id === selected.parentId) ?? null;
  }

  async function handleCreate(type: NodeType) {
    setError("");
    const parent = targetFolder();
    const parentKey = parent?.id ?? null;
    const name = uniqueName(list, parentKey, type);

    let node: TreeNode;
    if (type === "folder") {
      const r = await workspaceApi.createFolder(parent?.dbId ?? null, name);
      if (!r.success || !r.data) return setError(r.message);
      node = { id: folderKey(r.data.folderId), parentId: parentKey, type, name: r.data.name, order: r.data.sortOrder, dbId: r.data.folderId };
    } else {
      const r = await workspaceApi.createDocument(parent?.dbId ?? null, name);
      if (!r.success || !r.data) return setError(r.message);
      node = { id: docKey(r.data.documentId), parentId: parentKey, type, name: r.data.title, order: r.data.sortOrder, dbId: r.data.documentId };
    }

    setNodes((prev) => [...(prev ?? []), node]);
    setExpanded((prev) => {
      const next = new Set(prev);
      if (parentKey) next.add(parentKey);
      if (type === "folder") next.add(node.id);
      return next;
    });
    await flush();
    setSelectedId(node.id);
    if (type === "doc") setOpenDoc({ key: node.id, dbId: node.dbId, content: "" });
    setRenamingId(node.id); // 만들자마자 이름부터 정하게
  }

  async function handleRename(id: string, name: string) {
    setRenamingId(null);
    const node = list.find((n) => n.id === id);
    if (!node || node.name === name) return;
    const r =
      node.type === "folder"
        ? await workspaceApi.renameFolder(node.dbId, name)
        : await workspaceApi.updateDocument(node.dbId, { title: name });
    if (!r.success) return setError(r.message);
    setNodes((prev) => (prev ?? []).map((n) => (n.id === id ? { ...n, name } : n)));
  }

  async function handleDelete(id: string) {
    setDeletingId(null);
    const node = list.find((n) => n.id === id);
    if (!node) return;
    const r = node.type === "folder" ? await workspaceApi.deleteFolder(node.dbId) : await workspaceApi.deleteDocument(node.dbId);
    if (!r.success) return setError(r.message);

    const removed = descendantIds(list, id);
    setNodes((prev) => (prev ?? []).filter((n) => !removed.has(n.id)));
    if (selectedId && removed.has(selectedId)) setSelectedId(null);
    if (openDoc && removed.has(openDoc.key)) {
      pending.current = null; // 지워진 글은 저장하지 않는다
      setOpenDoc(null);
    }
  }

  function toggle(id: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  if (!user || nodes === null) {
    return <main className="flex flex-1 items-center justify-center text-gray-500">불러오는 중...</main>;
  }

  const deleting = deletingId ? list.find((n) => n.id === deletingId) : null;
  const deletingCount = deletingId ? descendantIds(list, deletingId).size - 1 : 0;
  const showEditor = selected?.type === "doc" && openDoc?.key === selected.id;

  return (
    <div className="workspace flex h-dvh flex-col bg-[var(--ws-editor)] text-[var(--ws-ink)]">
      {/* 맨 위 막대 */}
      <header className="flex items-center justify-between gap-3 border-b border-[var(--ws-line)] bg-[var(--ws-side)] px-4 py-2 text-sm">
        <Link href="/" className="font-bold">VoicePia</Link>
        <span className="truncate text-[var(--ws-muted)]">{user.penName} 작가님의 작업 공간</span>
      </header>

      <div className="flex min-h-0 flex-1 flex-col sm:flex-row">
        {/* 왼쪽: 탐색기 */}
        <aside
          className="flex max-h-[40dvh] min-h-0 flex-col border-b border-[var(--ws-line)] bg-[var(--ws-side)] sm:max-h-none sm:w-64 sm:border-r sm:border-b-0"
          onKeyDown={(e) => {
            if (!selected || renamingId) return;
            if (e.key === "F2") { e.preventDefault(); setRenamingId(selected.id); }
            if (e.key === "Delete") { e.preventDefault(); setDeletingId(selected.id); }
          }}
        >
          <div className="flex items-center justify-between px-3 py-2">
            <span className="text-[11px] font-semibold tracking-wider text-[var(--ws-muted)]">탐색기</span>
            <div className="flex gap-1">
              <ToolButton label="새 글" onClick={() => handleCreate("doc")}>
                <path d="M3.5 1.5H10L12.5 4V14.5H3.5Z" strokeLinejoin="round" />
                <path d="M8 7V12M5.5 9.5H10.5" />
              </ToolButton>
              <ToolButton label="새 폴더" onClick={() => handleCreate("folder")}>
                <path d="M1.5 3.5H6L7.5 5H14.5V12.5H1.5Z" strokeLinejoin="round" />
                <path d="M8 6.5V11M5.75 8.75H10.25" />
              </ToolButton>
              <ToolButton label="이름 바꾸기 (F2)" disabled={!selected} onClick={() => selected && setRenamingId(selected.id)}>
                <path d="M2.5 13.5L3 10.5L10.5 3L13 5.5L5.5 13Z" strokeLinejoin="round" />
              </ToolButton>
              <ToolButton label="삭제 (Delete)" disabled={!selected} onClick={() => selected && setDeletingId(selected.id)}>
                <path d="M2.5 4.5H13.5M6 4.5V2.5H10V4.5M4 4.5L4.8 14H11.2L12 4.5" strokeLinejoin="round" />
              </ToolButton>
            </div>
          </div>

          {error && (
            <p role="alert" className="mx-2 mb-2 rounded border border-red-400/60 bg-[var(--ws-editor)] p-2 text-xs text-red-500">
              {error}
            </p>
          )}

          {deleting && (
            <div role="alert" className="mx-2 mb-2 rounded border border-red-400/60 bg-[var(--ws-editor)] p-2 text-xs">
              <p>
                <b>{deleting.name}</b>
                {deletingCount > 0 ? `과(와) 안에 있는 ${deletingCount}개 항목을` : "을(를)"} 삭제할까요? 되돌릴 수 없어요.
              </p>
              <div className="mt-2 flex gap-2">
                <button className="rounded bg-red-600 px-2 py-1 text-white" onClick={() => handleDelete(deleting.id)}>삭제</button>
                <button className="rounded border border-[var(--ws-line)] px-2 py-1" onClick={() => setDeletingId(null)}>취소</button>
              </div>
            </div>
          )}

          <div className="min-h-0 flex-1 overflow-y-auto" tabIndex={0} aria-label="탐색기 목록">
            {list.length === 0 ? (
              <p className="px-4 py-3 text-xs leading-relaxed text-[var(--ws-muted)]">
                아직 아무것도 없어요. 위의 버튼으로 작품 폴더를 만들고, 그 안에 회차 글을 만들어 보세요.
              </p>
            ) : (
              <FileTree
                nodes={list}
                selectedId={selectedId}
                expanded={expanded}
                renamingId={renamingId}
                onSelect={select}
                onToggle={toggle}
                onStartRename={setRenamingId}
                onRename={handleRename}
                onCancelRename={() => setRenamingId(null)}
              />
            )}
          </div>
        </aside>

        {/* 오른쪽: 글 상자 */}
        <main className="flex min-h-0 min-w-0 flex-1 flex-col">
          {showEditor && selected && openDoc ? (
            <Editor
              docKey={openDoc.key}
              title={selected.name}
              content={openDoc.content}
              path={pathOf(list, selected.id)}
              saveState={saveState}
              onChange={handleContent}
            />
          ) : (
            <EmptyPane
              folder={selected?.type === "folder" ? selected : null}
              itemCount={selected ? childrenOf(list, selected.id).length : 0}
              onCreate={handleCreate}
            />
          )}
        </main>
      </div>
    </div>
  );
}

function EmptyPane(props: { folder: TreeNode | null; itemCount: number; onCreate: (t: NodeType) => void }) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 p-8 text-center text-sm text-[var(--ws-muted)]">
      {props.folder ? (
        <p>
          <b className="text-[var(--ws-ink)]">{props.folder.name}</b> 폴더 · 항목 {props.itemCount}개
          <br />왼쪽에서 글을 고르거나, 이 폴더에 새 글을 만들어 보세요.
        </p>
      ) : (
        <p>왼쪽에서 글을 고르거나 새로 만들어 보세요.</p>
      )}
      <div className="flex gap-2">
        <button className="rounded bg-[var(--ws-accent)] px-3 py-1.5 text-white" onClick={() => props.onCreate("doc")}>새 글</button>
        <button className="rounded border border-[var(--ws-line)] px-3 py-1.5 text-[var(--ws-ink)]" onClick={() => props.onCreate("folder")}>새 폴더</button>
      </div>
    </div>
  );
}

function ToolButton(props: { label: string; disabled?: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      title={props.label}
      aria-label={props.label}
      disabled={props.disabled}
      onClick={props.onClick}
      className="rounded p-1 text-[var(--ws-muted)] hover:bg-[var(--ws-hover)] hover:text-[var(--ws-ink)] disabled:opacity-30 disabled:hover:bg-transparent"
    >
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.3">
        {props.children}
      </svg>
    </button>
  );
}

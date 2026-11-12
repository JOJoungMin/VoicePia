"use client";

import { useEffect, useRef, useState } from "react";
import { childrenOf, type TreeNode } from "@/lib/workspace";

type Props = {
  nodes: TreeNode[];
  selectedId: string | null;
  expanded: Set<string>;
  renamingId: string | null;
  onSelect: (id: string) => void;
  onToggle: (id: string) => void;
  onStartRename: (id: string) => void;
  onRename: (id: string, name: string) => void;
  onCancelRename: () => void;
};

// 왼쪽 탐색기의 트리. 폴더를 펼치면 그 안의 항목을 한 단계씩 들여 써서 보여준다.
export default function FileTree(props: Props) {
  const roots = childrenOf(props.nodes, null);
  return (
    <ul role="tree" aria-label="폴더와 글" className="py-1 text-sm">
      {roots.map((node) => (
        <TreeItem key={node.id} node={node} depth={0} {...props} />
      ))}
    </ul>
  );
}

function TreeItem({ node, depth, ...props }: Props & { node: TreeNode; depth: number }) {
  const isFolder = node.type === "folder";
  const isOpen = props.expanded.has(node.id);
  const isSelected = props.selectedId === node.id;
  const isRenaming = props.renamingId === node.id;
  const children = isFolder && isOpen ? childrenOf(props.nodes, node.id) : [];

  return (
    <li role="treeitem" aria-expanded={isFolder ? isOpen : undefined} aria-selected={isSelected}>
      <div
        className={
          "group flex cursor-pointer items-center gap-1 py-[3px] pr-2 select-none " +
          (isSelected ? "bg-[var(--ws-selected)] text-[var(--ws-ink)]" : "hover:bg-[var(--ws-hover)]")
        }
        style={{ paddingLeft: 8 + depth * 14 }}
        onClick={() => {
          props.onSelect(node.id);
          if (isFolder) props.onToggle(node.id);
        }}
        onDoubleClick={() => props.onStartRename(node.id)}
      >
        <span className="w-3 shrink-0 text-center text-[10px] text-[var(--ws-muted)]">
          {isFolder ? (isOpen ? "▾" : "▸") : ""}
        </span>
        <span className="shrink-0 text-[var(--ws-muted)]" aria-hidden="true">
          {isFolder ? <FolderIcon open={isOpen} /> : <DocIcon />}
        </span>
        {isRenaming ? (
          <RenameInput
            initial={node.name}
            onDone={(name) => props.onRename(node.id, name)}
            onCancel={props.onCancelRename}
          />
        ) : (
          <span className="min-w-0 truncate">{node.name}</span>
        )}
      </div>
      {children.length > 0 && (
        <ul role="group">
          {children.map((child) => (
            <TreeItem key={child.id} node={child} depth={depth + 1} {...props} />
          ))}
        </ul>
      )}
    </li>
  );
}

// 이름 바꾸기 입력칸: Enter로 확정, Esc로 취소, 바깥을 누르면 확정
function RenameInput(props: { initial: string; onDone: (name: string) => void; onCancel: () => void }) {
  const [value, setValue] = useState(props.initial);
  const ref = useRef<HTMLInputElement>(null);
  const finished = useRef(false);

  useEffect(() => {
    ref.current?.focus();
    ref.current?.select();
  }, []);

  function finish(save: boolean) {
    if (finished.current) return;
    finished.current = true;
    const name = value.trim();
    if (save && name) props.onDone(name);
    else props.onCancel();
  }

  return (
    <input
      ref={ref}
      value={value}
      maxLength={100}
      aria-label="새 이름"
      onChange={(e) => setValue(e.target.value)}
      onClick={(e) => e.stopPropagation()}
      onKeyDown={(e) => {
        e.stopPropagation();
        if (e.key === "Enter") finish(true);
        if (e.key === "Escape") finish(false);
      }}
      onBlur={() => finish(true)}
      className="min-w-0 flex-1 rounded-sm border border-[var(--ws-accent)] bg-[var(--ws-editor)] px-1 text-sm outline-none"
    />
  );
}

function FolderIcon({ open }: { open: boolean }) {
  return (
    <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.3">
      {open ? (
        <path d="M1.5 4.5V12.5H12.5L14.5 7H4L2.5 11" strokeLinejoin="round" />
      ) : (
        <path d="M1.5 3.5H6L7.5 5H14.5V12.5H1.5Z" strokeLinejoin="round" />
      )}
    </svg>
  );
}

function DocIcon() {
  return (
    <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.3">
      <path d="M3.5 1.5H10L12.5 4V14.5H3.5Z" strokeLinejoin="round" />
      <path d="M5.5 7H10.5M5.5 9.5H10.5M5.5 12H8.5" />
    </svg>
  );
}

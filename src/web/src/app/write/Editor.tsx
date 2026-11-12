"use client";

import { countChars, type TreeNode } from "@/lib/workspace";

export type SaveState = "saved" | "saving" | "error";

type Props = {
  docKey: string;
  title: string;
  content: string | null;   // null = 서버에서 불러오는 중
  path: TreeNode[];
  saveState: SaveState;
  onChange: (content: string) => void;
};

// 오른쪽 글 상자. 위에는 경로(폴더 › 폴더 › 글), 아래에는 글자 수와 저장 상태.
export default function Editor({ docKey, title, content, path, saveState, onChange }: Props) {
  const { withSpaces, withoutSpaces } = countChars(content ?? "");

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex items-center gap-1 border-b border-[var(--ws-line)] px-4 py-2 text-xs text-[var(--ws-muted)]">
        {path.map((p, i) => (
          <span key={p.id} className={i === path.length - 1 ? "text-[var(--ws-ink)]" : ""}>
            {i > 0 && <span className="mx-1">›</span>}
            {p.name}
          </span>
        ))}
      </div>

      {content === null ? (
        <div className="flex flex-1 items-center justify-center text-sm text-[var(--ws-muted)]">불러오는 중…</div>
      ) : (
        <textarea
          key={docKey}
          value={content}
          onChange={(e) => onChange(e.target.value)}
          placeholder="여기에 글을 쓰세요."
          aria-label={`${title} 본문`}
          spellCheck={false}
          className="min-h-0 flex-1 resize-none bg-[var(--ws-editor)] px-8 py-6 text-[16px] leading-[1.9] text-[var(--ws-ink)] outline-none"
          style={{ fontFamily: "var(--ws-font-write)" }}
        />
      )}

      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-[var(--ws-line)] bg-[var(--ws-statusbar)] px-4 py-1 text-xs text-[var(--ws-muted)] tabular-nums">
        <span>
          공백 포함 <b className="text-[var(--ws-ink)]">{withSpaces.toLocaleString()}</b>자 · 공백 제외{" "}
          {withoutSpaces.toLocaleString()}자
        </span>
        <span>
          {saveState === "saving" && "저장 중…"}
          {saveState === "saved" && "저장됨"}
          {saveState === "error" && <span className="text-red-500">저장하지 못했어요. 인터넷 연결을 확인하세요.</span>}
        </span>
      </div>
    </div>
  );
}

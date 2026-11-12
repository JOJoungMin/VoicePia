"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { apiFetch, type UserInfo } from "@/lib/api";

export default function Home() {
  // undefined: 아직 확인 중 / null: 로그인 안 됨 / UserInfo: 로그인 됨
  const [user, setUser] = useState<UserInfo | null | undefined>(undefined);

  // 화면이 처음 뜰 때 한 번, 로그인 쿠키로 "나"를 조회한다
  useEffect(() => {
    apiFetch<UserInfo>("/api/auth/me").then((result) => {
      setUser(result.success ? result.data : null);
    });
  }, []);

  async function handleLogout() {
    await apiFetch<boolean>("/api/auth/logout", { method: "POST" });
    setUser(null);
  }

  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-6 px-4">
      <h1 className="text-3xl font-bold">VoicePia</h1>

      {user === undefined && <p className="text-gray-500">확인 중...</p>}

      {user === null && (
        <div className="flex gap-4">
          <Link href="/login" className="rounded bg-blue-600 px-4 py-2 text-white">
            로그인
          </Link>
          <Link href="/signup" className="rounded border border-gray-400 px-4 py-2">
            회원가입
          </Link>
        </div>
      )}

      {user && (
        <div className="flex flex-col items-center gap-3">
          <p>
            <strong>{user.penName}</strong> 작가님, 환영합니다.
          </p>
          <p className="text-sm text-gray-500">{user.email}</p>
          <div className="flex gap-3">
            <Link href="/write" className="rounded bg-blue-600 px-4 py-2 text-white">
              집필 시작
            </Link>
            <button onClick={handleLogout} className="rounded border border-gray-400 px-4 py-2">
              로그아웃
            </button>
          </div>
        </div>
      )}
    </main>
  );
}

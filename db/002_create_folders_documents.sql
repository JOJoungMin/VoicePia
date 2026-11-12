-- 002: 작성 화면의 폴더와 글
-- 실행 위치: voicepia DB
--
-- 폴더와 글을 테이블 두 개로 나눈다.
--   folders   : 폴더 안에 폴더를 무제한으로 넣을 수 있다 (parent_folder_id가 같은 테이블을 가리킴)
--   documents : 글은 어느 폴더에 속하는지만 가진다
-- 둘 다 부모가 NULL이면 맨 위 항목이다.

CREATE TABLE folders (
    folder_id         bigint       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id           bigint       NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    parent_folder_id  bigint       REFERENCES folders(folder_id) ON DELETE CASCADE,
    name              varchar(100) NOT NULL,
    sort_order        int          NOT NULL DEFAULT 0,
    created_at        timestamptz  NOT NULL DEFAULT now(),
    updated_at        timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_folders_not_own_parent CHECK (parent_folder_id <> folder_id)
);

CREATE TABLE documents (
    document_id  bigint       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id      bigint       NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    folder_id    bigint       REFERENCES folders(folder_id) ON DELETE CASCADE,
    title        varchar(100) NOT NULL,
    content      text         NOT NULL DEFAULT '',
    sort_order   int          NOT NULL DEFAULT 0,
    created_at   timestamptz  NOT NULL DEFAULT now(),
    updated_at   timestamptz  NOT NULL DEFAULT now()
);

-- "이 사람의, 이 폴더 바로 아래 항목들"을 빨리 찾기 위한 색인
CREATE INDEX ix_folders_user_parent  ON folders   (user_id, parent_folder_id);
CREATE INDEX ix_documents_user_folder ON documents (user_id, folder_id);

-- postgres 계정으로 만들었을 때 앱 계정이 접근할 수 있도록 주인을 넘긴다
ALTER TABLE folders   OWNER TO voicepia_app;
ALTER TABLE documents OWNER TO voicepia_app;

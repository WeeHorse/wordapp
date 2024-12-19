create database database_for_wordapp
    with owner postgres;

create table public.words
(
    word     varchar not null,
    clientid text
);

alter table public.words
    owner to postgres;

create unique index words_word_uindex
    on public.words (word);

create index words_clientid
    on public.words (clientid);


import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from '../../shared/ui/button/button';
import { Logo } from '../../shared/ui/brand/logo';
import { Link } from '../../shared/ui/link/link';
import { SHELL_ROUTES } from '../../shell/nav-items';
import { AUTH_ROUTES } from '../auth/auth-paths';
import { AuthService } from '../auth/auth.service';

const SECTION = 'flex flex-col gap-2.5';
const SECTION_TITLE = 'font-serif text-dialog-title font-semibold text-ink';
const LIST = 'flex list-disc flex-col gap-1.5 pl-5 text-body text-ink-2 marker:text-ink-muted';

/**
 * The privacy page (PLAN.md §7): what is stored, who sees it, what goes to AI providers and how to take it
 * out or delete it. Public, so it can be read before creating an account. Its statements follow ADR-0006
 * (CV text), ADR-0008 (AI providers), ADR-0013 (community numbers) and ADR-0015 (deletion and export);
 * change this page when one of those changes.
 */
@Component({
  selector: 'app-privacy-page',
  imports: [RouterLink, Button, Link, Logo],
  host: { class: 'flex min-h-dvh flex-col bg-base' },
  template: `
    <header
      class="flex h-15 shrink-0 items-center gap-2.25 border-b border-rule bg-surface pr-2 pl-4.5 lg:px-7"
    >
      <app-logo [size]="24" />
      <span class="flex-1 font-serif text-dialog-title font-semibold text-ink">Heardback</span>
      <a appButton [routerLink]="back().path" class="max-lg:h-11">{{ back().label }}</a>
    </header>

    <main class="flex w-full max-w-170 flex-col gap-7 px-4 pt-6 pb-12 lg:px-7 lg:pt-8">
      <div class="flex flex-col gap-2">
        <h1 class="font-serif text-page-title-sm font-semibold text-ink lg:text-page-title">
          Privacy
        </h1>
        <p class="text-small text-ink-muted">
          Last updated <span class="font-mono">{{ lastUpdated }}</span>
        </p>
        <p class="text-body text-ink-2">
          Heardback is a small, invite-only tracker for job applications. This page explains what it
          keeps about you, who can see it, what is sent to AI providers, and how to take your data
          out or delete it.
        </p>
      </div>

      <section aria-labelledby="privacy-stored" [class]="section">
        <h2 id="privacy-stored" [class]="sectionTitle">What Heardback keeps</h2>
        <ul [class]="list">
          <li>
            <span class="text-ink">Your account:</span> your name, your email and a secure hash of
            your password. The password itself is never stored.
          </li>
          <li>
            <span class="text-ink">Your applications:</span> company, role, job link, job
            description, how and when you applied, the timeline and your notes.
          </li>
          <li>
            <span class="text-ink">Your CV, as text only:</span> email addresses, phone numbers,
            links and address lines are replaced before it is stored, and the PDF itself is never
            saved. Also the skills found in it and the ones you removed.
          </li>
          <li>
            <span class="text-ink">AI results:</span> job summaries, CV suggestions, and whether you
            allowed CV suggestions.
          </li>
          <li>
            <span class="text-ink">Sign-in sessions</span> so you stay signed in, and counts of how
            many AI requests you made today, for the daily limits.
          </li>
          <li>
            Server logs record what happened with ids only, never your email, your CV or your notes.
          </li>
        </ul>
      </section>

      <section aria-labelledby="privacy-who" [class]="section">
        <h2 id="privacy-who" [class]="sectionTitle">Who can see it</h2>
        <ul [class]="list">
          <li>Only you see your applications, notes, CV and AI suggestions.</li>
          <li>
            Admins see the list of members: names, emails and when they joined, so they can help
            with a forgotten password or email. They never see your applications or your CV.
          </li>
          <li>
            When several members add the same job, Heardback links their applications to one shared
            record of that job so it can count them. Nobody sees another member’s copy of it, their
            notes or their CV.
          </li>
          <li>
            “Stats from other members” are counts only, shown once at least 3 other members applied
            to the same job or company. You are never counted in your own numbers, and no names,
            roles or job descriptions of other members are shown.
          </li>
        </ul>
      </section>

      <section aria-labelledby="privacy-ai" [class]="section">
        <h2 id="privacy-ai" [class]="sectionTitle">What is sent to AI providers</h2>
        <p class="text-body text-ink-2">
          Nothing is sent unless you ask for it. Personal details are filtered out of the text
          first.
        </p>
        <ul [class]="list">
          <li>
            <span class="text-ink">Summarize job</span> sends the job description to Groq. If Groq
            is unavailable, it goes to the free tier of Google Gemini, where Google may use it to
            improve its products and people may read it. Keep the job description to the job ad
            itself.
          </li>
          <li>
            <span class="text-ink">CV suggestions</span> send your CV text and the job description
            to Groq, only after you allow it. They are never sent to a provider that may use them to
            train models. You can stop this at any time on the CV page, which also deletes the
            suggestions.
          </li>
          <li>
            Groq does not train on what it receives. It may keep requests for up to 30 days to check
            for abuse.
          </li>
        </ul>
      </section>

      <section aria-labelledby="privacy-browser" [class]="section">
        <h2 id="privacy-browser" [class]="sectionTitle">Cookies and this browser</h2>
        <ul [class]="list">
          <li>
            One cookie keeps you signed in. Scripts on the page can’t read it, and it is only sent
            to Heardback.
          </li>
          <li>Your theme choice is saved in this browser.</li>
          <li>
            No analytics, ads or trackers. Fonts come from Heardback itself, so no request goes to
            Google Fonts.
          </li>
        </ul>
      </section>

      <section aria-labelledby="privacy-choices" [class]="section">
        <h2 id="privacy-choices" [class]="sectionTitle">Taking your data out or deleting it</h2>
        <ul [class]="list">
          <li>
            <span class="text-ink">Download everything</span> as one JSON file from
            <a appLink [routerLink]="settingsPath">Settings</a>, under “Your data”.
          </li>
          <li>
            <span class="text-ink">Delete one thing:</span> an application from its details, your CV
            or the permission for CV suggestions on the CV page.
          </li>
          <li>
            <span class="text-ink">Delete your account</span> from Settings. Your account and
            everything in it are deleted straight away, and the stats stop counting you at once. The
            shared record of a job stays only while another member’s application still uses it.
          </li>
        </ul>
      </section>

      <section aria-labelledby="privacy-questions" [class]="section">
        <h2 id="privacy-questions" [class]="sectionTitle">Questions</h2>
        <p class="text-body text-ink-2">Ask the admin who invited you to Heardback.</p>
      </section>
    </main>
  `,
})
export class PrivacyPage {
  private readonly auth = inject(AuthService);

  protected readonly lastUpdated = '25 Nov 2026';
  protected readonly settingsPath = SHELL_ROUTES.settings;
  protected readonly section = SECTION;
  protected readonly sectionTitle = SECTION_TITLE;
  protected readonly list = LIST;

  /** Back into the app when signed in, to sign in otherwise. */
  protected readonly back = computed(() =>
    this.auth.isAuthenticated()
      ? { label: 'Go to Applications', path: SHELL_ROUTES.applications }
      : { label: 'Sign in', path: AUTH_ROUTES.login },
  );
}

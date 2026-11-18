/**
 * Tailwind's `lg` breakpoint (docs/design.md §9): from here the sidebar shows and dialogs are
 * centred; below it the mobile layout and bottom sheets are used. Keep in step with Tailwind's 64rem.
 */
export const DESKTOP_MEDIA_QUERY = '(min-width: 64rem)';

/**
 * Tailwind's `xl` breakpoint (docs/design.md §9): from here the application detail opens as a panel
 * beside the list; below it as a full-screen sheet. Keep in step with Tailwind's 80rem.
 */
export const WIDE_MEDIA_QUERY = '(min-width: 80rem)';

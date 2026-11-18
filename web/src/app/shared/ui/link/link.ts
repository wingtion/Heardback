import { Directive } from '@angular/core';

/** Text link style (docs/design.md §3 `link`): underlined, with the standard focus ring. */
@Directive({
  selector: 'a[appLink]',
  host: {
    class:
      'rounded-xs text-link underline underline-offset-2 focus-visible:outline-2 ' +
      'focus-visible:outline-offset-2 focus-visible:outline-focus',
  },
})
export class Link {}

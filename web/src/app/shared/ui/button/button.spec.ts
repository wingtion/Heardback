import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Button } from './button';

@Component({
  imports: [Button],
  template: `
    <button appButton type="button" class="w-full">Cancel</button>
    <button appButton="primary" size="lg" type="submit">Sign in</button>
    <button appButton="ghost" iconOnly size="xl" type="button" aria-label="Open menu"></button>
  `,
})
class ButtonHost {}

describe('Button', () => {
  function buttons(): HTMLButtonElement[] {
    const fixture = TestBed.createComponent(ButtonHost);
    fixture.detectChanges();
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
  }

  it('uses the secondary style for a bare appButton and keeps classes set in the template', () => {
    const [cancel] = buttons();

    expect(cancel.classList).toContain('border-field-border');
    expect(cancel.classList).toContain('w-full');
  });

  it('applies the requested variant and size', () => {
    const [, signIn] = buttons();

    expect(signIn.classList).toContain('bg-accent');
    expect(signIn.classList).toContain('h-11');
  });

  it('makes icon-only buttons square without padding', () => {
    const [, , menu] = buttons();

    expect(menu.classList).toContain('size-12');
    expect(menu.classList).toContain('p-0');
    expect(menu.classList).not.toContain('px-4');
  });
});

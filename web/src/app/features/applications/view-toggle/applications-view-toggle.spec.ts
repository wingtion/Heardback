import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ApplicationsView, ApplicationsViewToggle } from './applications-view-toggle';

@Component({
  imports: [ApplicationsViewToggle],
  template: `<app-applications-view-toggle
    [value]="view()"
    (valueChange)="changes.push($event)"
  />`,
})
class ToggleHost {
  readonly view = signal<ApplicationsView>('list');
  readonly changes: ApplicationsView[] = [];
}

describe('ApplicationsViewToggle', () => {
  function render(): { host: ToggleHost; buttons: HTMLButtonElement[] } {
    const fixture = TestBed.createComponent(ToggleHost);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    return {
      host: fixture.componentInstance,
      buttons: Array.from(element.querySelectorAll('button')),
    };
  }

  it('render_ListSelected_MarksOnlyListPressed', () => {
    const { buttons } = render();

    expect(buttons.map((button) => button.textContent?.trim())).toEqual(['List', 'Board']);
    expect(buttons.map((button) => button.getAttribute('aria-pressed'))).toEqual(['true', 'false']);
  });

  it('click_OtherView_EmitsIt', () => {
    const { host, buttons } = render();

    buttons[1].click();

    expect(host.changes).toEqual(['board']);
  });

  it('click_CurrentView_EmitsNothing', () => {
    const { host, buttons } = render();

    buttons[0].click();

    expect(host.changes).toEqual([]);
  });
});

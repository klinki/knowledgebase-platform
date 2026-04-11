import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-shell-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './shell-not-found.component.html',
  styleUrl: './shell-not-found.component.scss'
})
export class ShellNotFoundComponent {
}
